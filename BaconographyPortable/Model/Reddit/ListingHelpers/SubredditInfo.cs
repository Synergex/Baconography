using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class SubredditInfo : IListingProvider
    {
        IRedditService _redditService;

        public SubredditInfo(IBaconProvider baconProvider)
        {
            _redditService = baconProvider.GetService<IRedditService>();
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            state["SubscribedSubreddits"] = await _redditService.GetSubscribedSubreddits();
            return await _redditService.GetSubreddits(null);
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://www.reddit.com/reddits", after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }
    }
}
