using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    public class SubredditInfo : IListingProvider
    {
        IRedditService _redditService;
        IOfflineService _offlineService;
        IUserService _userService;
        public SubredditInfo(IBaconProvider baconProvider)
        {
            _redditService = baconProvider.GetService<IRedditService>();
            _offlineService = baconProvider.GetService<IOfflineService>();
            _userService = baconProvider.GetService<IUserService>();
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            var sublist = await _redditService.GetSubscribedSubreddits();
            if(sublist != null)
                state["SubscribedSubreddits"] = sublist;
           

            var subreddits = await _redditService.GetSubreddits(null);
            subreddits.Data.Children.Insert(0, ThingUtility.GetFrontPageThing());
            return subreddits;
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://www.reddit.com/reddits", after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }


        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return GetInitialListing(state);
        }
    }
}
