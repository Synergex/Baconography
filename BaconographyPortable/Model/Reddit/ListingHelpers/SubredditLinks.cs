using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class SubredditLinks : IListingProvider, ICachedListingProvider
    {
        IRedditService _redditService;
        IOfflineService _offlineService;
        string _subreddit;
        string _subredditId;
        string _permaLink;

        public SubredditLinks(IBaconProvider baconProvider, string subreddit, string subredditId = null)
        {
            _redditService = baconProvider.GetService<IRedditService>();
            _offlineService = baconProvider.GetService<IOfflineService>();
            _subreddit = subreddit;
            _subredditId = subredditId;
        }

        public Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return _redditService.GetPostsBySubreddit(_subreddit, null);
        }

        public async Task CacheIt(Listing listing)
        {
            if (listing != null && listing.Data.Children != null && listing.Data.Children.Count > 0)
                await _offlineService.StoreOrderedThings("links:" + _subreddit, listing.Data.Children);
        }

        public async Task<Listing> GetCachedListing(Dictionary<object, object> state)
        {
            var things = await _offlineService.RetrieveOrderedThings("links:" + _subreddit, TimeSpan.FromDays(14));
            if(things != null)
                return new Listing { Data = new ListingData { Children = new List<Thing>(things) } };
            else
                return new Listing { Data = new ListingData { Children = new List<Thing>() } };
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


        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return GetInitialListing(state);
        }
    }
}
