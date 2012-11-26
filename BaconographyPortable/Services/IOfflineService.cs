using BaconographyPortable.Model.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IOfflineService
    {
        Task Clear();
        //an async method that returns async loaded image tuples
        Task<IEnumerable<Task<Tuple<string, byte[]>>>> GetImages(string uri);

        Task StoreComment(Thing comment);
        Task StoreComments(Listing listing);
        Task<Listing> GetTopLevelComments(string subredditId, string linkId, int count);
        Task<Listing> GetMoreComments(string after, int count);

        Task StoreLink(Thing link);
        Task StoreLinks(Listing listing);
        Task<Listing> LinksForSubreddit(string subredditName, string after);
        Task<Listing> AllLinks(string after);

        Task StoreOrderedThings(string key, IEnumerable<Thing> things);
        Task<IEnumerable<Thing>> RetrieveOrderedThings(string key);

        Task StoreOrderedThings(IListingProvider listingProvider);
    }
}
