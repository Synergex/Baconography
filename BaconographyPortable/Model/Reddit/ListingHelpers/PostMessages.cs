using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class PostMessages : IListingProvider
    {
        IRedditService _redditService;

        public PostMessages(IBaconProvider baconProvider)
        {
            _redditService = baconProvider.GetService<IRedditService>();
        }

        public Tuple<Task<Listing>, Func<Task<Listing>>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Func<Task<Listing>>>(null, () => _redditService.GetMessages(100));
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
