using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class SubredditSubscriptions : IListingProvider
    {
        IUserService _userService;
        IRedditService _redditService;
        IOfflineService _offlineService;
        public SubredditSubscriptions(IBaconProvider baconProvider)
        {
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _offlineService = baconProvider.GetService<IOfflineService>();
        }

        public Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return _redditService.GetSubscribedSubredditListing();
        }

        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return GetInitialListing(state);
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://www.reddit.com/reddits/mine.json", after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }
    }
}
