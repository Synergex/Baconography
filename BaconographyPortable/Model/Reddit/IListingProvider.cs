using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    public interface IListingProvider
    {
        Tuple<Task<Listing>, Task<Listing>> GetInitialListing(Dictionary<object, object> state);
        Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state);
        Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state);
    }
}
