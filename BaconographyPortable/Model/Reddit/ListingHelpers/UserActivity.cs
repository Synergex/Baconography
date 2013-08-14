using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class UserActivity : IListingProvider
    {
        IRedditService _redditService;
        string _username;

        public UserActivity(IBaconProvider baconProvider, string username)
        {
            _redditService = baconProvider.GetService<IRedditService>();
            _username = username;
        }

        public Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return _redditService.GetPostsByUser(_username, null);
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://reddit.com/user/" + _username, after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }


        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return _redditService.GetPostsByUser(_username, null);
        }
    }
}
