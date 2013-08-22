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
        Task<Listing> Search(string query, int? limit, bool reddits, string restrictedToSubreddit);
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
        Task<Listing> GetMessages(int? limit);
        Task<Thing> GetLinkByUrl(string url);
        Task<Listing> GetAdditionalFromListing(string baseUrl, string after, int? limit);
        Task<TypedThing<Account>> GetAccountInfo(string accountName);
        Task AddVote(string thingId, int direction);
        Task AddSubredditSubscription(string subreddit, bool unsub);
        Task AddSavedThing(string thingId);
        Task AddReportOnThing(string thingId);
        Task AddPost(string kind, string url, string text, string subreddit, string title);
        Task AddMessage(string recipient, string subject, string message);
        Task AddComment(string parentId, string content);
        Task EditComment(string thingId, string text);
        Task SubmitCaptcha(string captcha);
        void AddFlairInfo(string linkId, string opName);
        Task ReadMessage(string id);

        AuthorFlairKind GetUsernameModifiers(string username, string linkid, string subreddit);
    }
}
