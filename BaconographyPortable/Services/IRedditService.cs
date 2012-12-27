using BaconographyPortable.Model.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IRedditService
    {
        Task<Account> GetMe();
        Task<Account> GetMe(User user);
        Task<bool> CheckLogin(string loginToken);
        Task<User> Login(string username, string password);
        Task<Listing> Search(string query, int? limit);
        Task<Thing> GetThingById(string id);
        Task<HashSet<string>> GetSubscribedSubreddits();
        Task<Listing> GetSubscribedSubredditListing();
        Task<Listing> GetDefaultSubreddits();
        Task<Listing> GetSubreddits(int? limit);
        Task<TypedThing<Subreddit>> GetSubreddit(string name);
        Task<Listing> GetPostsByUser(string username, int? limit);
        Task<Listing> GetPostsBySubreddit(string subreddit, int? limit);
        Task<Listing> GetMoreOnListing(IEnumerable<string> childrenIds, string contentId, string subreddit);
        Task<Listing> GetCommentsOnPost( string subreddit, string permalink, int? limit);
        Task<Thing> GetLinkByUrl(string url);
        Task<Listing> GetAdditionalFromListing(string baseUrl, string after, int? limit);
        Task<TypedThing<Account>> GetAccountInfo(string accountName);
        void AddVote(string thingId, int direction);
        void AddSubredditSubscription(string subreddit, bool unsub);
        void AddSavedThing(string thingId);
        void AddReportOnThing(string thingId);
        void AddPost(string kind, string url, string subreddit, string title);
        void AddMessage(string recipient, string subject, string message);
        void AddComment(string parentId, string content);
        void AddFlairInfo(string linkId, string opName);

        AuthorFlairKind GetUsernameModifiers(string username, string linkid, string subreddit);
    }
}
