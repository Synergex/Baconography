using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public abstract class ThingViewModelCollection : BaseIncrementalLoadCollection<ViewModelBase>
    {
        protected IRedditService _redditService;
        protected INavigationService _navigationService;
        protected IUserService _userService;
        protected IBaconProvider _baconProvider;
        protected ISettingsService _settingsService;
        protected IListingProvider _onlineListingProvider;
        protected IListingProvider _offlineListingProvider;
        protected ISuspendableWorkQueue _suspendableWorkQueue;

        public ThingViewModelCollection(IBaconProvider baconProvider, IListingProvider onlineListingProvider, IListingProvider offlineListingProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _settingsService = _baconProvider.GetService<ISettingsService>();
            _suspendableWorkQueue = _baconProvider.GetService<ISuspendableWorkQueue>();
            _onlineListingProvider = onlineListingProvider;
            _offlineListingProvider = offlineListingProvider;
        }

        protected override async Task<IEnumerable<ViewModelBase>> InitialLoad(Dictionary<object, object> state)
        {
            return MapListing(await GetInitialListing(state), state);
        }
        bool _hasLoadedAdditional = false;
        protected override async Task<IEnumerable<ViewModelBase>> LoadAdditional(Dictionary<object, object> state)
        {
            if (state.ContainsKey("After"))
            {
                var after = state["After"] as string;
                state.Remove("After");

                return MapListing(await GetAdditionalListing(after, state), state);
            }
            else if (state.ContainsKey("More"))
            {
                var more = state["More"] as IEnumerable<string>;
                var targetMore = more.Take(500)
                    .ToList();

                //asking for 500 of anything is probably unreasonable but reddit will sort it out on the other side in the most efficiant way possible
                //but there isnt any sense in asking for more then 500 when thats the max number of items they're going to return us
                if (targetMore.Count == 500)
                    state["More"] = more.Skip(500).ToList();
                else
                    state.Remove("More");

                return MapListing(await GetMore(targetMore, state), state);
            }
            else
                throw new NotImplementedException();
        }

        private HashSet<string> _ids = new HashSet<string>();
        protected virtual IEnumerable<ViewModelBase> MapListing(Listing listing, Dictionary<object, object> state)
        {
            if (listing.Data.After != null)
            {
                state["After"] = listing.Data.After;
            }
            lock (_ids)
            {
                return listing.Data.Children
                    .Select(thing => MapThing(thing, state))
                    .Where(vmb => vmb != null);
            }
        }

        protected virtual ViewModelBase MapThing(Thing thing, Dictionary<object, object> state)
        {
            if (thing.Data is Link)
            {
                if (_ids.Contains(((Link)thing.Data).Id))
                    return null;
                else
                    _ids.Add(((Link)thing.Data).Id);

                var linkView = new LinkViewModel(thing, _baconProvider);
                if (state.ContainsKey("MultiRedditSource"))
                    linkView.FromMultiReddit = true;
                return linkView;
            }
            else if (thing.Data is Comment)
                return new CommentViewModel(_baconProvider, thing, ((Comment)thing.Data).LinkId, false);
            else if (thing.Data is Subreddit)
            {
                var isSubscribed = state.ContainsKey("SubscribedSubreddits") ?
                    ((HashSet<string>)state["SubscribedSubreddits"]).Contains(((Subreddit)thing.Data).Name) :
                    false;
                return new AboutSubredditViewModel(_baconProvider, thing, isSubscribed);
            }
            else if (thing.Data is Message)
            {
                return new MessageViewModel(_baconProvider, thing);
            }
            else if (thing.Data is More)
            {
                //multiple 'more's can come back from reddit and we should add them to the list for load additional to ask for
                object moreState;
                if (state.TryGetValue("More", out moreState))
                {
                    //sometimes they give us duplicates make sure we remove them right away
                    var moreList = moreState as IEnumerable<string>;
                    if (moreList != null)
                    {
                        state["More"] = moreList.Concat(((More)thing.Data).Children)
                            .Distinct()
                            .ToList();
                    }
                    else
                    {
                        state["More"] = ((More)thing.Data).Children
                            .Distinct()
                            .ToList();
                    }
                }
                return null;
            }
            else if (thing.Data is Advertisement)
                return new AdvertisementViewModel(_baconProvider);
            else
                throw new NotImplementedException();
        }

        protected override bool HasAdditional(Dictionary<object, object> state)
        {
            return (state.ContainsKey("After") && state["After"] is string) || 
                (state.ContainsKey("More") && state["More"] is string);
        }

        private async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            if (_settingsService.IsOnline())
            {
                Listing initialListing = null;
                if (_onlineListingProvider is ICachedListingProvider)
                {
                    initialListing = await ((ICachedListingProvider)_onlineListingProvider).GetCachedListing(state);
                }
                else
                {
                    initialListing = await _offlineListingProvider.GetInitialListing(state);
                }
                if (!(_offlineListingProvider is IDontRefreshAutomatically))
                {
                    BackgroundUpdate(state);
                }
                return initialListing;
            }
            else
                return await _offlineListingProvider.GetInitialListing(state);
        }

        private void BackgroundUpdate(Dictionary<object, object> state)
        {
            _suspendableWorkQueue.QueueInteruptableUI(async (token) =>
            {
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                var targetListing = await _onlineListingProvider.GetInitialListing(state);

                if (token.IsCancellationRequested)
                    return;

                if (targetListing != null)
                {
                    ViewModelBase[] mappedListing;
                    lock (_ids)
                    {
                        foreach (var vm in this)
                        {
                            var linkViewModel = vm as LinkViewModel;
                            if (linkViewModel != null)
                            {
                                _ids.Remove(linkViewModel.Id);
                            }
                        }
                        mappedListing = MapListing(targetListing, state).ToArray();
                    }

                    //remove the ones we're not replacing, otherwise we end up with state results
                    if (Count > mappedListing.Length)
                    {
                        for (int i = Count - 1; i >= mappedListing.Length; i--)
                        {
                            RemoveAt(i);
                        }
                    }

                    for (int i = 0; i < mappedListing.Length; i++)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        if (Count > i)
                        {
                            if (this[i] is IMergableThing)
                            {
                                if (((IMergableThing)this[i]).MaybeMerge(mappedListing[i]))
                                    continue;
                            }
                            this[i] = mappedListing[i];
                        }
                        else
                            Add(mappedListing[i]);
                    }

                    if (_onlineListingProvider is ICachedListingProvider)
                    {
                        try
                        {
                            await _suspendableWorkQueue.QueueLowImportanceRestartableWork(async (token2) => await ((ICachedListingProvider)_onlineListingProvider).CacheIt(targetListing));
                        }
                        catch (TaskCanceledException)
                        {
                        }
                    }
                }
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            });
        }

        private Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            if (_settingsService.IsOnline())
                return _onlineListingProvider.GetAdditionalListing(after, state);
            else
                return _offlineListingProvider.GetAdditionalListing(after, state);
        }

        private Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            if (_settingsService.IsOnline())
                return _onlineListingProvider.GetMore(ids, state);
            else
                return _offlineListingProvider.GetMore(ids, state);
        }

        protected override void Refresh(Dictionary<object, object> state)
        {
            BackgroundUpdate(state);
        }
    }
}
