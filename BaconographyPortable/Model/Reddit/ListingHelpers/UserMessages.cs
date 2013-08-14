using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class UserMessages : IListingProvider
    {
        IRedditService _redditService;

        public UserMessages(IBaconProvider baconProvider)
        {
            _redditService = baconProvider.GetService<IRedditService>();
        }

        public Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return _redditService.GetMessages(100);
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return _redditService.GetMessages(100);
        }
    }
}
