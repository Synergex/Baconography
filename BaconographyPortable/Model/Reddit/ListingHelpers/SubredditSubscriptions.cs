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
        public SubredditSubscriptions(IBaconProvider baconProvider)
        {
            _userService = baconProvider.GetService<IUserService>();
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            var user = await _userService.GetUser();
            if (user != null && user.Me != null)
            {
                return await _redditService.GetSubscribedSubredditListing();
            }
            else
            {
                return await _redditService.GetDefaultSubreddits();
            }
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
