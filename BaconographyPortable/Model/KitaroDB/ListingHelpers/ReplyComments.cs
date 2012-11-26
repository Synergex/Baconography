using BaconographyPortable.Model.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.KitaroDB.ListingHelpers
{
    class ReplyComments : IListingProvider
    {
        IEnumerable<Thing> _things;

        public ReplyComments(IEnumerable<Thing> things)
        {
            _things = things;
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return new Listing { Kind = "Listing", Data = new ListingData { Children = _things.ToList() } };
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }
    }
}
