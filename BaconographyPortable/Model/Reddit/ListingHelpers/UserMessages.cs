using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class UserMessages : IListingProvider, ICachedListingProvider
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

        //we arent really a caching provide we just dont want the collection to load from offline if we arent offline
        public Task<Listing> GetCachedListing(Dictionary<object, object> state)
        {
            return Task.FromResult(new Listing { Data = new ListingData { Children = new List<Thing>() } });
        }

        public async Task CacheIt(Listing listing)
        {
            
        }
    }
}
