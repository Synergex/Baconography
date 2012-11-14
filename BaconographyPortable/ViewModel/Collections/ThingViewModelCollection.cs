using BaconographyPortable.Common;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
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

        public ThingViewModelCollection(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
        }

        protected override async Task<IEnumerable<ViewModelBase>> InitialLoad(Dictionary<object, object> state)
        {
            //state["CurrentUser"] = await _userService.GetUser();
            //state["SubscribedSubreddits"] = await _redditService.GetSubscribedSubreddits();
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
        }

        private IEnumerable<ViewModelBase> MapListing(Listing listing, Dictionary<object, object> state)
        {
            if (listing.Data.After != null)
            {
                state["After"] = listing.Data.After;
            }

            return listing.Data.Children
                .Select(thing => MapThing(thing, state))
                .Where(vmb => vmb != null);
        }

        private ViewModelBase MapThing(Thing thing, Dictionary<object, object> state)
        {
            if (thing.Data is Link)
                return new LinkViewModel(thing, _baconProvider);
            else if (thing.Data is Comment)
                return new CommentViewModel(_baconProvider, thing, ((Comment)thing.Data).LinkId, true, string.Empty);
            else if (thing.Data is Subreddit)
                return new AboutSubredditViewModel(_baconProvider, thing, ((HashSet<string>)state["SubscribedSubreddits"]).Contains(((Subreddit)thing.Data).Name));
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
            else
                throw new NotImplementedException();
        }

        protected override bool HasAdditional(Dictionary<object, object> state)
        {
            return state.ContainsKey("After") || state.ContainsKey("More");
        }

        protected abstract Task<Listing> GetInitialListing(Dictionary<object, object> state);
        protected abstract Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state);
        protected abstract Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state);
    }
}
