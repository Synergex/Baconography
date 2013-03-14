using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class SubredditLinks : IListingProvider
    {
        IRedditService _redditService;
        IOfflineService _offlineService;
        string _subreddit;
        string _subredditId;

        public SubredditLinks(IBaconProvider baconProvider, string subreddit, string subredditId = null)
        {
            _redditService = baconProvider.GetService<IRedditService>();
            _offlineService = baconProvider.GetService<IOfflineService>();
            _subreddit = subreddit;
            _subredditId = subredditId;
        }

        public Tuple<Task<Listing>, Task<Listing>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Task<Listing>>(GetCachedListing(), GetUncachedListing());
        }

        private async Task<Listing> GetUncachedListing()
        {
            var resultListing = await _redditService.GetPostsBySubreddit(_subreddit, null);
            //doesnt need to be awaited let it run in the background
            
            _offlineService.StoreOrderedThings("links:" + _subreddit, resultListing.Data.Children);
            return resultListing;

        }

        private async Task<Listing> GetCachedListing()
        {
            var things = await _offlineService.RetrieveOrderedThings("links:" + _subreddit);
            return new Listing { Data = new ListingData { Children = new List<Thing>(things) } };
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://reddit.com" + _subreddit, after, null);
        }

        public async Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            if (string.IsNullOrEmpty(_subredditId))
            {
                var subredditThing = await _redditService.GetSubreddit(_subreddit);
                _subredditId = subredditThing.Data.Name;
            }

            return await _redditService.GetMoreOnListing(ids, _subredditId, _subreddit);
        }
    }
}
