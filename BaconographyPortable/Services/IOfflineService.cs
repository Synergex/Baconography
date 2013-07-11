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
        //an async method that returns async loaded image info tuples
        Task<IEnumerable<Tuple<string, string>>> GetImages(string uri);
        Task<byte[]> GetImage(string uri);
        Task StoreImage(byte[] bytes, string uri);
        Task StoreImages(IEnumerable<Tuple<string, string>> apiResults, string uri);

        Task StoreComments(Listing listing);
        Task<Listing> GetTopLevelComments(string permalink, int count);
        Task<Listing> GetMoreComments(string subredditId, string linkId, IEnumerable<string> ids);

        Task StoreMessages(User user, Listing listing);
        Task<Listing> GetMessages(User user);
        Task<bool> UserHasOfflineMessages(User user);

        Task IncrementDomainStatistic(string domain, bool isLink);
        Task IncrementSubredditStatistic(string subredditId, bool isLink);
        Task<List<DomainAggregate>> GetDomainAggregates(int maxListSize, int threshold);
        Task<List<SubredditAggregate>> GetSubredditAggregates(int maxListSize, int threshold);

        Task StoreLink(Thing link);
        Task StoreLinks(Listing listing);
        Task<Listing> LinksForSubreddit(string subredditName, string after);
        Task<Listing> AllLinks(string after);

        Task StoreThing(string key, Thing link);
        Task<Thing> RetrieveThing(string key, TimeSpan maxAge);
        Task StoreOrderedThings(string key, IEnumerable<Thing> things);
        Task<IEnumerable<Thing>> RetrieveOrderedThings(string key, TimeSpan maxAge);

        Task<TypedThing<Link>> RetrieveLink(string id);
        Task<TypedThing<Link>> RetrieveLinkByUrl(string url, TimeSpan maxAge);
        Task<TypedThing<Subreddit>> RetrieveSubredditById(string id);

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
        Task StoreSubreddit(TypedThing<Subreddit> subreddit);
        uint GetHash(string name);
    }
}
