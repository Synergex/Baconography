using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    public interface IListingProvider
    {
        Task<Listing> GetInitialListing(Dictionary<object, object> state);
        Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state);
        Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state);
        Task<Listing> Refresh(Dictionary<object, object> state);
    }

    public interface IDontRefreshAutomatically
    {
    }

    public interface ICachedListingProvider
    {
        Task<Listing> GetCachedListing(Dictionary<object, object> state);
        Task CacheIt(Listing listing);
    }
}
