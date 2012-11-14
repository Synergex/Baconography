using BaconographyPortable.Properties;
using BaconographyPortable.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    public class RedditService : IRedditService
    {
        ISettingsService _settingsService;
        IOfflineService _offlineService;
        ISimpleHttpService _simpleHttpService;
        IUserService _userService;
        INotificationService _notificationService;

        public RedditService(ISettingsService settingsService, IOfflineService offlineService, ISimpleHttpService simpleHttpService, IUserService userService, INotificationService notificationService)
        {
            _settingsService = settingsService;
            _offlineService = offlineService;
            _simpleHttpService = simpleHttpService;
            _userService = userService;
            _notificationService = notificationService;
        }

        public async Task<Account> GetMe()
        {
            var user = await _userService.GetUser();
            return await GetMe(user);
        }

        //this one is seperated out so we can use it interally on initial user login
        private async Task<Account> GetMe(User user)
        {
            try
            {
                var thing = JsonConvert.DeserializeObject<Thing>(await _simpleHttpService.SendGet(user.LoginCookie, "http://www.reddit.com/api/me.json"));
                return (new TypedThing<Account>(thing)).Data;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new Account { Name = user.Username };
            }
        }

        public async Task<User> Login(string username, string password)
        {
            var loginUri = "http://www.reddit.com/api/login/" + username;
            var postContent = new Dictionary<string, string>
                {
                    { "api_type", "json" },
                    { "user", username },
                    { "passwd", password }
                };

            var loginResult = await _simpleHttpService.SendPostForCookies(postContent, loginUri);

            var jsonResult = loginResult.Item1;
            var loginResultThing = JsonConvert.DeserializeObject<LoginJsonThing>(jsonResult);
            if (loginResultThing.Json == null ||
                (loginResultThing.Json.Errors != null && loginResultThing.Json.Errors.Length != 0))
            {
                _notificationService.CreateNotification(string.Format("Failed to login as User:{0}", username));
                return null; //errors in the login process
            }
            else
            {
                var loginCookie = loginResult.Item2["reddit_session"];
                var user = new User { Authenticated = true, LoginCookie = loginCookie, Username = username };

                user.Me = await GetMe(user);
                return user;
            }

        }

        public async Task<Listing> Search(string query, int? limit)
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 100;
            var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

            var targetUri = string.Format("http://www.reddit.com/search.json?limit={0}&q={1}",
                                           guardedLimit,
                                           query);

            var comments = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);
            var newListing = JsonConvert.DeserializeObject<Listing>(comments);
            return MaybeFilterForNSFW(newListing);
        }

        public async Task<Thing> GetThingById(string id)
        {
            var targetUri = string.Format("http://www.reddit.com/by_id/{0}.json", id);

            try
            {
                var thingStr = await _simpleHttpService.UnAuthedGet(targetUri);
                return JsonConvert.DeserializeObject<Thing>(thingStr);
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return null;
            }
        }

        public async Task<HashSet<string>> GetSubscribedSubreddits()
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 100;

            var targetUri = string.Format("http://www.reddit.com/reddits/mine.json?limit={0}", maxLimit);

            var hashifyListing = new Func<Thing, string>((thing) =>
                {
                    if (thing.Data is Subreddit)
                    {
                        return ((Subreddit)thing.Data).Name;
                    }
                    else
                        return null;
                });

            try
            {
                var subreddits = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);

                if (subreddits == "\"{}\"")
                    return new HashSet<string>(JsonConvert.DeserializeObject<Listing>(Resources.DefaultSubreddits)
                        .Data.Children.Select(hashifyListing)
                        .Where(str => str != null));
                else
                    return new HashSet<string>(JsonConvert.DeserializeObject<Listing>(subreddits)
                        .Data.Children.Select(hashifyListing)
                        .Where(str => str != null));
                            
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new HashSet<string>(JsonConvert.DeserializeObject<Listing>(Resources.DefaultSubreddits)
                        .Data.Children.Select(hashifyListing)
                        .Where(str => str != null));
            }
        }

        public async Task<Listing> GetSubreddits(int? limit)
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 100;
            var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

            var targetUri = string.Format("http://www.reddit.com/reddits/.json?limit={0}", guardedLimit);

            try
            {
                var comments = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);
                var newListing = JsonConvert.DeserializeObject<Listing>(comments);

                return MaybeFilterForNSFW(newListing);
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public async Task<TypedThing<Subreddit>> GetSubreddit(string name)
        {
            var targetUri = string.Format("http://www.reddit.com/r/{0}/about.json", name);

            try
            {
                var comments = await _simpleHttpService.UnAuthedGet(targetUri);
                return new TypedThing<Subreddit>(JsonConvert.DeserializeObject<Thing>(comments));
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new TypedThing<Subreddit>(new Thing { Kind = "t5", Data = new Subreddit { Headertitle = name } });
            }
        }

        public async Task<Listing> GetPostsByUser(string username, int? limit)
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 100;
            var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

            var targetUri = string.Format("http://www.reddit.com/user/{0}/.json?limit={1}", username, guardedLimit);

            try
            {
                var comments = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);
                var newListing = JsonConvert.DeserializeObject<Listing>(comments);

                return MaybeFilterForNSFW(newListing);
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public async Task<Listing> GetPostsBySubreddit(string subreddit, int? limit)
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 100;
            var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

            var targetUri = string.Format("http://www.reddit.com/r/{0}/.json?limit={1}", subreddit, guardedLimit);
            try
            {
                var comments = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);
                var newListing = JsonConvert.DeserializeObject<Listing>(comments);
                return MaybeFilterForNSFW(newListing);
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public async Task<Listing> GetMoreOnListing(IEnumerable<string> childrenIds, string contentId, string subreddit)
        {
            var targetUri = "http://www.reddit.com/api/morechildren.json";

            var arguments = new Dictionary<string, string>
            {
                {"children", string.Join(",", childrenIds) },
                {"link_id", contentId },
                {"pv_hex", ""},
                {"api_type", "json" }
            };

            if (subreddit != null)
            {
                arguments.Add("r", subreddit);
            }

            try
            {
                var result = await _simpleHttpService.SendPost(await GetCurrentLoginCookie(), arguments, targetUri);
                var newListing = new Listing
                {
                    Kind = "Listing",
                    Data = new ListingData { Children = JsonConvert.DeserializeObject<JsonThing>(result).Json.Data.Things }
                };

                return MaybeFilterForNSFW(newListing);
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);

                return new Listing
                {
                    Kind = "Listing",
                    Data = new ListingData { Children = new List<Thing>() }
                };
            }
        }

        public async Task<Listing> GetCommentsOnPost(string subreddit, string permalink, int? limit)
        {
            try
            {
                var maxLimit = (await UserIsGold()) ? 1500 : 500;
                var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

                var targetUri = limit == -1 ?
                            string.Format("http://www.reddit.com{0}.json", permalink) :
                            string.Format("http://www.reddit.com{0}.json?limit={1}", permalink, limit);

                Listing listing = null;
                var comments = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);
                if (comments.StartsWith("["))
                {
                    var listings = JsonConvert.DeserializeObject<Listing[]>(comments);
                    listing = new Listing { Data = new ListingData { Children = new List<Thing>() } };
                    foreach (var combinableListing in listings)
                    {
                        listing.Data.Children.AddRange(combinableListing.Data.Children);
                        listing.Kind = combinableListing.Kind;
                        listing.Data.After = combinableListing.Data.After;
                        listing.Data.Before = combinableListing.Data.Before;
                    }
                }
                else
                    listing = JsonConvert.DeserializeObject<Listing>(comments);

                return MaybeFilterForNSFW(listing);
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public async Task<Listing> GetAdditionalFromListing(string baseUrl, string after, int? limit)
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 500;
            var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

            string targetUri = null;
            //if this base url already has arguments (like search) just append the count and the after
            if (baseUrl.Contains(".json?"))
                targetUri = string.Format("{0}&count={1}&after={2}", baseUrl, guardedLimit, after);
            else
                targetUri = string.Format("{0}.json?count={1}&after={2}", baseUrl, guardedLimit, after);

            try
            {
                var listing = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);
                var newListing = JsonConvert.DeserializeObject<Listing>(listing);

                return MaybeFilterForNSFW(newListing);
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public async Task<TypedThing<Account>> GetAccountInfo(string accountName)
        {
            var targetUri = string.Format("http://www.reddit.com/user/{0}/about.json", accountName);

            try
            {
                var account = await _simpleHttpService.UnAuthedGet(targetUri);
                return new TypedThing<Account>(JsonConvert.DeserializeObject<Thing>(account));
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new TypedThing<Account>(new Thing { Kind = "t3", Data = new Account { Name = accountName } });
            }
        }

        public async void AddVote(string thingId, int direction)
        {
            var modhash = await GetCurrentModhash();

            var arguments = new Dictionary<string, string>
            {
                {"id", thingId},
                {"dir", direction.ToString()},
                {"uh", modhash}
            };

            var result = await _simpleHttpService.SendPost(await GetCurrentLoginCookie(), arguments, "http://www.reddit.com/api/vote");
        }

        public async void AddSubredditSubscription(string subreddit, bool unsub)
        {
            var modhash = await GetCurrentModhash();
            await _simpleHttpService.SendPost(await GetCurrentLoginCookie(),
                string.Format("sr={0}&uh={1}&r={2}&renderstyle={3}&action={4}", subreddit, modhash, subreddit, "html", unsub ? "unsub" : "sub"),
                "http://www.reddit.com/api/subscribe");
        }

        public async void AddSavedThing(string thingId)
        {
            var modhash = await GetCurrentModhash();
            var targetUri = "http://www.reddit.com/api/save";

            var content = new Dictionary<string, string>
            {
                { "id", thingId},
                { "uh", modhash}
            };

            await _simpleHttpService.SendPost(await GetCurrentLoginCookie(), content, targetUri);
        }

        public async void AddReportOnThing(string thingId)
        {
            var modhash = await GetCurrentModhash();
            var targetUri = "http://www.reddit.com/api/report";

            var content = new Dictionary<string, string>
            {
                { "id", thingId},
                { "uh", modhash}
            };

            await _simpleHttpService.SendPost(await GetCurrentLoginCookie(), content, targetUri);
        }

        public async void AddPost(string kind, string url, string subreddit, string title)
        {
            var modhash = await GetCurrentModhash();
            await _simpleHttpService.SendPost(await GetCurrentLoginCookie(),
                string.Format("uh={0}&kind={1}&url={2}&sr={3}&title={4}&r={3}&renderstyle=html", modhash, kind, url, subreddit, title),
                "http://www.reddit.com/api/submit");
        }

        public async void AddMessage(string recipient, string subject, string message)
        {
            var modhash = await GetCurrentModhash();
            await _simpleHttpService.SendPost(await GetCurrentLoginCookie(),
                string.Format("id={0}&uh={1}&to={2}&text={3}&subject={4}&thing-id={5}&renderstyle={6}", "#compose-message", modhash, recipient, message, subject, "", "html"),
                "http://www.reddit.com/api/compose");
        }

        public async void AddComment(string parentId, string content)
        {
            var modhash = await GetCurrentModhash();
            await _simpleHttpService.SendPost(await GetCurrentLoginCookie(),
                string.Format("thing_id={0}&text={1}&uh={2}", parentId, content.Replace("\r\n", "\n"), modhash),
                "http://www.reddit.com/api/comment");
        }

        private async Task<bool> UserIsGold()
        {
            var user = await _userService.GetUser();
            return user != null && user.Me != null && user.Me.IsGold;
        }

        private async Task<string> GetCurrentLoginCookie()
        {
            var currentUser = await _userService.GetUser();
            if (currentUser != null && currentUser.Authenticated)
            {
                return currentUser.LoginCookie;
            }
            else
                return string.Empty;
        }

        private async Task<string> GetCurrentModhash()
        {
            var currentUser = await _userService.GetUser();
            if (currentUser != null && currentUser.Authenticated && currentUser.Me != null)
            {
                return currentUser.Me.ModHash;
            }
            else
                return string.Empty;
        }

        private Listing MaybeFilterForNSFW(Listing source)
        {
            if (_settingsService.AllowOver18)
            {
                return source;
            }
            else
                return FilterForNSFW(source);
        }

        private Listing FilterForNSFW(Listing source)
        {
            source.Data.Children = source.Data.Children
                .Select(FilterForNSFW)
                .Where(thing => thing != null)
                .ToList();

            return source;
        }

        private Thing FilterForNSFW(Thing source)
        {
            if (source.Data is Link || source.Data is Subreddit)
            {
                if (((dynamic)source.Data).Over18)
                    return null;
            }

            return source;
        }
    }
}
