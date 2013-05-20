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

        public ThingViewModelCollection(IBaconProvider baconProvider, IListingProvider onlineListingProvider, IListingProvider offlineListingProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _settingsService = _baconProvider.GetService<ISettingsService>();
            _onlineListingProvider = onlineListingProvider;
            _offlineListingProvider = offlineListingProvider;
        }

        protected override async Task<IEnumerable<ViewModelBase>> InitialLoad(Dictionary<object, object> state)
        {
            return MapListing(await GetInitialListing(state), state);
        }

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

        protected virtual IEnumerable<ViewModelBase> MapListing(Listing listing, Dictionary<object, object> state)
        {
            if (listing.Data.After != null)
            {
                state["After"] = listing.Data.After;
            }

            return listing.Data.Children
                .Select(thing => MapThing(thing, state))
                .Where(vmb => vmb != null);
        }

        protected virtual ViewModelBase MapThing(Thing thing, Dictionary<object, object> state)
        {
            if (thing.Data is Link)
            {
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
                var initTpl = _onlineListingProvider.GetInitialListing(state);

                if (initTpl.Item1 != null)
                {
					var initCache = await initTpl.Item1;
                    Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                    Task.Run(async () =>
                    {
                        Listing target = null;
                        try
                        {
                            await Task.Delay(1000);
                            target = await initTpl.Item2();
                        }
                        catch
                        {
                        }
                        if (target == null || !_settingsService.IsOnline())
                            target = await _offlineListingProvider.GetInitialListing(state).Item2();
                        return target;
                    }).ContinueWith(async (result) =>
                        {
                            var targetListing = result.Result;
                            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                            if (targetListing != null)
                            {
                                var mappedListing = MapListing(targetListing, state).ToArray();
								if (mappedListing.Length < this.Count)
									this.Clear();
                                for (int i = 0; i < mappedListing.Length; i++)
                                {
                                    if (this.Count > i)
                                        this[i] = mappedListing[i];
                                    else
                                        Add(mappedListing[i]);
                                }


                            }
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    return initCache;
                }
                else
                {
                    var result = await initTpl.Item2();
                    //make sure we arent starting up in offline mode
                    if (_settingsService.IsOnline())
                    {
                        return result;
                    }
                    else
                        return await _offlineListingProvider.GetInitialListing(state).Item2();
                }
            }
            else
                return await _offlineListingProvider.GetInitialListing(state).Item2();
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

        protected override Task Refresh(Dictionary<object, object> state)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });

            return Task.Run(async () =>
                {
                    Listing target = null;
                    try
                    {
                        if (_settingsService.IsOnline())
                            target = await _onlineListingProvider.Refresh(state);
                    }
                    catch
                    {
                    }
                    if (target == null || !_settingsService.IsOnline())
                        target = await _offlineListingProvider.Refresh(state);
                    return target;
                }).ContinueWith((result) =>
                {
                    var targetListing = result.Result;
                    Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                    if (targetListing != null)
                    {
                        var mappedListing = MapListing(targetListing, state).ToArray();
						if (mappedListing.Length < this.Count)
							this.Clear();

                        for (int i = 0; i < mappedListing.Length; i++)
                        {
                            if (this.Count > i)
                                this[i] = mappedListing[i];
                            else
                                Add(mappedListing[i]);
                        }
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
