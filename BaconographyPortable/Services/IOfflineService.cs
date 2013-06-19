using BaconographyPortable.Model.KitaroDB.ListingHelpers;
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

        Task StoreComments(Listing listing);
        Task<Listing> GetTopLevelComments(string subredditId, string linkId, int count);
        Task<Listing> GetMoreComments(string subredditId, string linkId, IEnumerable<string> ids);

        Task IncrementDomainStatistic(string domain, bool isLink);
        Task IncrementSubredditStatistic(string subredditId, bool isLink);
        Task<List<DomainAggregate>> GetDomainAggregates(int maxListSize, int threshold);
        Task<List<SubredditAggregate>> GetSubredditAggregates(int maxListSize, int threshold);

        Task StoreLink(Thing link);
        Task StoreLinks(Listing listing);
        Task<Listing> LinksForSubreddit(string subredditName, string after);
        Task<Listing> AllLinks(string after);

        Task StoreOrderedThings(string key, IEnumerable<Thing> things);
        Task<IEnumerable<Thing>> RetrieveOrderedThings(string key);

        Task StoreOrderedThings(IListingProvider listingProvider);

        Task StoreSetting(string name, string value);
        Task<string> GetSetting(string name);

        Task StoreHistory(string link);
        Task ClearHistory();
        bool HasHistory(string link);

        Task Suspend();

        Task EnqueueAction(string actionName, Dictionary<string, string> parameters);
        Task<Tuple<string, Dictionary<string, string>>> DequeueAction();

        Task<Thing> GetSubreddit(string name);
        uint GetHash(string name);
    }
}
