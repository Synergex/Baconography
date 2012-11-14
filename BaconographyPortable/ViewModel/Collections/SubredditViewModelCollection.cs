using BaconographyPortable.Common;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class SubredditViewModelCollection : BaseIncrementalLoadCollection<AboutSubredditViewModel>
    {
        IRedditService _redditService;
        IBaconProvider _baconProvider;

        public SubredditViewModelCollection(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
        }

        protected override async Task<IEnumerable<AboutSubredditViewModel>> InitialLoad(Dictionary<object, object> state)
        {
            state["SubscribedSubreddits"] = await _redditService.GetSubscribedSubreddits();

            return MapListing(await _redditService.GetSubreddits(null), state);
        }

        protected async override Task<IEnumerable<AboutSubredditViewModel>> LoadAdditional(Dictionary<object, object> state)
        {
            //if we dont add a new after this needs to be the end
            var after = state["After"] as string;
            state.Remove("After");

            return MapListing(await _redditService.GetAdditionalFromListing("http://www.reddit.com/reddits", after, null), state);
        }

        private IEnumerable<AboutSubredditViewModel> MapListing(Listing listing, Dictionary<object, object> state)
        {
            if (listing.Data.After != null)
            {
                state["After"] = listing.Data.After;
            }

            var subscribedSubreddits = state["SubscribedSubreddits"] as HashSet<string>;

            return listing.Data.Children
                .Where(thing => thing.Data is Subreddit)
                .Select(thing => new AboutSubredditViewModel(_baconProvider, thing, subscribedSubreddits.Contains(((Subreddit)thing.Data).Name)));
        }

        protected override bool HasAdditional(Dictionary<object, object> state)
        {
            return state.ContainsKey("After");
        }
    }
}
