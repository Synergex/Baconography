using BaconographyPortable.Properties;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    public class RedditService : IRedditService
    {
        protected ISettingsService _settingsService;
        protected ISimpleHttpService _simpleHttpService;
        protected IUserService _userService;
        protected INotificationService _notificationService;
        protected IBaconProvider _baconProvider;

        Dictionary<string, string> _linkToOpMap = new Dictionary<string, string>();
        Dictionary<string, HashSet<string>> _subredditToModMap = new Dictionary<string, HashSet<string>>();

        public virtual void Initialize(ISettingsService settingsService, ISimpleHttpService simpleHttpService, IUserService userService, INotificationService notificationService, IBaconProvider baconProvider)
        {
            _settingsService = settingsService;
            _simpleHttpService = simpleHttpService;
            _userService = userService;
            _notificationService = notificationService;
            _baconProvider = baconProvider;
        }

        public async Task<Account> GetMe()
        {
            var user = await _userService.GetUser();
            return await GetMe(user);
        }

        //this one is seperated out so we can use it interally on initial user login
        public async Task<Account> GetMe(User user)
        {
            try
            {
                var meString = await _simpleHttpService.SendGet(user.LoginCookie, "http://www.reddit.com/api/me.json");
                if (!string.IsNullOrWhiteSpace(meString) && meString != "{}")
                {
                    var thing = JsonConvert.DeserializeObject<Thing>(meString);
                    return (new TypedThing<Account>(thing)).Data;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return null;
            }
        }

        public async Task<User> Login(string username, string password)
        {
            var loginUri = "https://ssl.reddit.com/api/login";
            var postContent = new Dictionary<string, string>
                {
                    { "api_type", "json" },
                    { "user", username },
                    { "passwd", password }
                };

            var loginResult = await _simpleHttpService.SendPostForCookies(postContent, loginUri);

            var jsonResult = loginResult.Item1;
            var loginResultThing = JsonConvert.DeserializeObject<LoginJsonThing>(jsonResult);
			if (loginResultThing == null || loginResultThing.Json == null ||
                (loginResultThing.Json.Errors != null && loginResultThing.Json.Errors.Length != 0))
            {
                _notificationService.CreateNotification(string.Format("Failed to login as User:{0}", username));
                return null; //errors in the login process
            }
            else
            {
                var loginCookie = loginResult.Item2["reddit_session"];
                var user = new User { Authenticated = true, LoginCookie = loginCookie, Username = username, NeedsCaptcha = false };

                user.Me = await GetMe(user);
                return user;
            }

        }

        public async Task<Listing> Search(string query, int? limit, bool reddits)
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 100;
            var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

            var targetUri = string.Format(
                reddits ? 
                    "http://www.reddit.com/subreddits/search.json?limit={0}&q={1}" : 
                    "http://www.reddit.com/search.json?limit={0}&q={1}",
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
                if(thingStr.StartsWith("{\"kind\": \"Listing\""))
                {
                    var listing = JsonConvert.DeserializeObject<Listing>(thingStr);
                    return listing.Data.Children.First();
                }
                else
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
            var hashifyListing = new Func<Thing, string>((thing) =>
                {
                    if (thing.Data is Subreddit)
                    {
                        return ((Subreddit)thing.Data).Name;
                    }
                    else
                        return null;
                });

            return new HashSet<string>((await GetSubscribedSubredditListing())
                    .Data.Children.Select(hashifyListing)
                    .Where(str => str != null));
            
        }

        public virtual async Task<Listing> GetSubreddits(int? limit)
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
            //no info for the front page
            if (name == "/")
                return new TypedThing<Subreddit>(new Thing { Kind = "t5", Data = new Subreddit { Headertitle = name } });
            else if (name == "all")
                return new TypedThing<Subreddit>(new Thing { Kind = "t5", Data = new Subreddit { Headertitle = "all", Url = "/r/all", Name = "all", DisplayName="all", Title="all", Id="t5_fakeid" } });

            var targetUri = string.Format("http://www.reddit.com/r/{0}/about.json", name);

            try
            {
                var comments = await _simpleHttpService.UnAuthedGet(targetUri);
                //error page
                if (comments.ToLower().StartsWith("<!doctype html>"))
                {
                    return new TypedThing<Subreddit>(new Thing { Kind = "t5", Data = new Subreddit { Headertitle = name, Title = name, Url = string.Format("r/{0}", name), Created = DateTime.Now, CreatedUTC = DateTime.UtcNow, DisplayName = name, Description = "there doesnt seem to be anything here", Name = name, Over18 = false, PublicDescription = "there doesnt seem to be anything here", Subscribers = 0 } });
                }
                else
                {
                    return new TypedThing<Subreddit>(JsonConvert.DeserializeObject<Thing>(comments));
                }
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
            var maxLimit = (await UserIsGold()) ? 1500 : 25;
            var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

            if (subreddit == null)
            {
                //this isnt the front page, that would be "/"
                //return empty since there isnt anything here
                _notificationService.CreateNotification("There doesnt seem to be anything here");
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }

            var targetUri = string.Format("http://www.reddit.com{0}.json?limit={1}", subreddit, guardedLimit);
            try
            {
                var links = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);
				var newListing = JsonConvert.DeserializeObject<Listing>(links);
                return MaybeFilterForNSFW(MaybeInjectAdvertisements(newListing));
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

            if (childrenIds.Count() == 0)
                return new Listing
                {
                    Kind = "Listing",
                    Data = new ListingData()
                };

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

                return MaybeFilterForNSFW(MaybeInjectAdvertisements(newListing));
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

        Tuple<DateTime, string, string, Listing> _lastCommentsOnPostRequest;

        public async Task<Thing> GetLinkByUrl(string url)
        {
            try
            {
                var originalUrl = url;
                url = url + ".json";
                Listing listing = null;
                var comments = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), url);
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

                var requestedLinkInfo = listing.Data.Children.FirstOrDefault(thing => thing.Data is Link);
                if (requestedLinkInfo != null)
                {

                    var result = MaybeFilterForNSFW(listing);

                    ((Link)requestedLinkInfo.Data).Permalink = originalUrl;
                    _lastCommentsOnPostRequest = Tuple.Create(DateTime.Now, ((Link)requestedLinkInfo.Data).Subreddit, ((Link)requestedLinkInfo.Data).Permalink, result);
                    return requestedLinkInfo;
                }
                else
                    return null;
            }
            catch(Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return null;
            }
        }

        public async Task<Listing> GetCommentsOnPost(string subreddit, string permalink, int? limit)
        {
            //comments are pretty slow to get, so cache it to within 5 minutes for the most recent request
            if (_lastCommentsOnPostRequest != null &&
                (DateTime.Now - _lastCommentsOnPostRequest.Item1).TotalMinutes < 5 &&
                _lastCommentsOnPostRequest.Item2 == subreddit &&
                _lastCommentsOnPostRequest.Item3 == permalink)
                return _lastCommentsOnPostRequest.Item4;

            try
            {
                var maxLimit = (await UserIsGold()) ? 1500 : 500;
                var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

                string targetUri = null;

                if (permalink.Contains(".json?"))
                {
                    targetUri = "http://www.reddit.com" + permalink;
                }
                else
                {
                    targetUri = limit == -1 ?
                                string.Format("http://www.reddit.com{0}.json", permalink) :
                                string.Format("http://www.reddit.com{0}.json?limit={1}", permalink, limit);
                }

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

                var result = MaybeFilterForNSFW(listing);

                var requestedLinkInfo = listing.Data.Children.FirstOrDefault(thing => thing.Data is Link);
                if (requestedLinkInfo != null)
                {
                    if (!_linkToOpMap.ContainsKey(((Link)requestedLinkInfo.Data).Name))
                    {
                        _linkToOpMap.Add(((Link)requestedLinkInfo.Data).Name, ((Link)requestedLinkInfo.Data).Author);
                    }
                }

                _lastCommentsOnPostRequest = Tuple.Create(DateTime.Now, subreddit, permalink, result);

                return result;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public async Task<Listing> GetMessages(int? limit)
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 100;
            var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

            var targetUri = string.Format("http://www.reddit.com/message/inbox/.json?limit={0}", guardedLimit);

            try
            {
                var messages = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);
                // Hacky hack mcHackerson
                messages = messages.Replace("\"kind\": \"t1\"", "\"kind\": \"t4.5\"");
                return JsonConvert.DeserializeObject<Listing>(messages);
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public void AddFlairInfo(string linkId, string opName)
        {
            if (!_linkToOpMap.ContainsKey(linkId))
            {
                _linkToOpMap.Add(linkId, opName);
            }
        }

        public async Task<Listing> GetAdditionalFromListing(string baseUrl, string after, int? limit)
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 500;
            var guardedLimit = Math.Min(maxLimit, limit ?? maxLimit);

            string targetUri = null;
            //if this base url already has arguments (like search) just append the count and the after
            if (baseUrl.Contains(".json?"))
                targetUri = string.Format("{0}&limit={1}&after={2}", baseUrl, guardedLimit, after);
            else
                targetUri = string.Format("{0}.json?limit={1}&after={2}", baseUrl, guardedLimit, after);

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

        public virtual async Task AddVote(string thingId, int direction)
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

        public virtual async Task AddSubredditSubscription(string subreddit, bool unsub)
        {
            var modhash = await GetCurrentModhash();

            var content = new Dictionary<string, string>
            {
                { "sr", subreddit},
                { "uh", modhash},
                { "r", subreddit},
                { "renderstyle", "html"},
                { "action", unsub ? "unsub" : "sub"}
            };

            await _simpleHttpService.SendPost(await GetCurrentLoginCookie(), content, "http://www.reddit.com/api/subscribe");
        }

        public virtual async Task AddSavedThing(string thingId)
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

        public virtual async Task AddReportOnThing(string thingId)
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

        public virtual async Task AddPost(string kind, string url, string subreddit, string title)
        {
            var modhash = await GetCurrentModhash();

            var arguments = new Dictionary<string, string>
            {
                {"api_type", "json"},
                {"kind", kind},
                {"url", url},
                {"title", title},
                {"r", subreddit},
                {"renderstyle", "html" },
                {"uh", modhash}
            };

            await this.SendPost(await GetCurrentLoginCookie(), arguments, "http://www.reddit.com/api/submit");
        }

        public async Task SubmitCaptcha(string captcha)
        {
            Captcha = captcha;
            List<PostData> data = PostQueue.ToList<PostData>();
            PostQueue.Clear();
            foreach (var post in data)
            {
                await this.SendPost(post.Cookie, post.UrlEncodedData, post.Uri, true);
            }
        }

        private class PostData
        {
            public string Cookie { get; set; }
            public Dictionary<string, string> UrlEncodedData { get; set; }
            public string Uri { get; set; }
        }
        private List<PostData> PostQueue = new List<PostData>();

        private string CaptchaIden { get; set; }
        private string Captcha { get; set; }
        private async Task<string> SendPost(string cookie, Dictionary<string, string> urlEncodedData, string uri, bool queuedMessage = false)
        {
            if (!urlEncodedData.ContainsKey("api_type"))
                urlEncodedData.Add("api_type", "json");

            if (!String.IsNullOrEmpty(CaptchaIden))
            {
                if (urlEncodedData.ContainsKey("iden"))
                    urlEncodedData["iden"] = CaptchaIden;
                else
                    urlEncodedData.Add("iden", CaptchaIden);
            }

            if (!String.IsNullOrEmpty(Captcha))
            {
                if (urlEncodedData.ContainsKey("captcha"))
                    urlEncodedData["captcha"] = Captcha;
                else
                    urlEncodedData.Add("captcha", Captcha);
            }

            string response = null;

            response = await _simpleHttpService.SendPost(cookie, urlEncodedData, uri);

            var jsonObject = JsonConvert.DeserializeObject(response) as JObject;
            JToken captcha = null;
            JToken errors = null;
            JObject first = null;

            if (jsonObject.First != null)
                first = (jsonObject.First as JProperty).Value as JObject;

            if (first != null)
            {
                first.TryGetValue("captcha", out captcha);
                first.TryGetValue("errors", out errors);
                if (captcha != null)
                    CaptchaIden = captcha.Value<string>();

                if (captcha != null && errors != null)
                {
                    var user = await _userService.GetUser();
                    user.NeedsCaptcha = true;

                    // If a user has told us to bug off this session, do as they say
                    if (!_settingsService.PromptForCaptcha)
                        return response;

                    PostQueue.Add(new PostData { Cookie = cookie, Uri = uri, UrlEncodedData = urlEncodedData });

                    CaptchaViewModel captchaVM = CaptchaViewModel.GetInstance(_baconProvider);
                    captchaVM.ShowCaptcha(CaptchaIden);
                }
            }

            return response;
        }

        public virtual async Task AddMessage(string recipient, string subject, string message)
        {
            var modhash = await GetCurrentModhash();

            var arguments = new Dictionary<string, string>
            {
                {"id", "#compose-message"},
                {"to", recipient},
                {"text", message},
                {"subject", subject},
                {"thing-id", ""},
                {"renderstyle", "html"},
                {"uh", modhash}
            };

            var temp = await this.SendPost(await GetCurrentLoginCookie(), arguments, "http://www.reddit.com/api/compose");
        }

        public virtual async Task AddReply(string recipient, string subject, string message, string thing_id)
        {
            var modhash = await GetCurrentModhash();

            var arguments = new Dictionary<string, string>
            {
                {"id", "#compose-message"},
                {"to", recipient},
                {"text", message},
                {"subject", subject},
                {"thing-id", ""},
                {"renderstyle", "html"},
                {"uh", modhash}
            };

            var temp = await this.SendPost(await GetCurrentLoginCookie(), arguments, "http://www.reddit.com/api/compose");
        }

        public virtual async Task AddComment(string parentId, string content)
        {
            var modhash = await GetCurrentModhash();

            var arguments = new Dictionary<string, string>
            {
                {"thing_id", parentId},
                {"text", content.Replace("\r\n", "\n")},
                {"uh", modhash}
            };

            var result = await this.SendPost(await GetCurrentLoginCookie(), arguments, "http://www.reddit.com/api/comment");
        }

        public virtual async Task EditComment(string thingId, string text)
        {
            var modhash = await GetCurrentModhash();

            var arguments = new Dictionary<string, string>
            {
                {"thing_id", thingId},
                {"text", text.Replace("\r\n", "\n")},
                {"uh", modhash}
            };

            var result = await this.SendPost(await GetCurrentLoginCookie(), arguments, "http://www.reddit.com/api/editusertext");
        }

        private async Task<bool> UserIsGold()
        {
            var user = await _userService.GetUser();
            return user != null && user.Me != null && user.Me.IsGold;
        }

        private async Task<string> GetCurrentLoginCookie()
        {
            var currentUser = await _userService.GetUser();
            if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.LoginCookie))
            {
                return currentUser.LoginCookie;
            }
            else
                return string.Empty;
        }

        private async Task<string> GetCurrentModhash()
        {
            var currentUser = await _userService.GetUser();
            if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.LoginCookie) && currentUser.Me != null)
            {
                return currentUser.Me.ModHash;
            }
            else if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.LoginCookie))
            {
                currentUser.Me = await GetMe();
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

		private Listing MaybeInjectAdvertisements(Listing source)
		{
			return source;

			int count = source.Data.Children.Count;
			for (int i = 9; i < count; i += 10)
			{
				var thing = new Thing { Data = new Advertisement(), Kind = "ad" };
				source.Data.Children.Insert(i, thing);
			}
			return source;
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


        public AuthorFlairKind GetUsernameModifiers(string username, string linkid, string subreddit)
        {
            if (!string.IsNullOrEmpty(linkid))
            {
                string opName;
                if (_linkToOpMap.TryGetValue(linkid, out opName) && opName == username)
                {
                    return AuthorFlairKind.OriginalPoster;
                }
            }

            if (!string.IsNullOrEmpty(subreddit))
            {
                HashSet<string> subredditMods;
                if (_subredditToModMap.TryGetValue(subreddit, out subredditMods) && subredditMods != null && subredditMods.Contains(username))
                {
                    return AuthorFlairKind.Moderator;
                }
            }

            return AuthorFlairKind.None;
        }


        public async Task<Listing> GetSubscribedSubredditListing()
        {
            var maxLimit = (await UserIsGold()) ? 1500 : 100;

            var targetUri = string.Format("http://www.reddit.com/reddits/mine.json?limit={0}", maxLimit);

            try
            {
                var subreddits = await _simpleHttpService.SendGet(await GetCurrentLoginCookie(), targetUri);

                if (subreddits == "\"{}\"")
                    return await GetDefaultSubreddits();
                else
                    return JsonConvert.DeserializeObject<Listing>(subreddits);

            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
            }
            //cant await in a catch block so do it after
            return await GetDefaultSubreddits();
        }

        public async Task<Listing> GetDefaultSubreddits()
        {
            return JsonConvert.DeserializeObject<Listing>(Resources.DefaultSubreddits1 + Resources.DefaultSubreddits2 + Resources.DefaultSubreddits3);
        }


        public async Task<bool> CheckLogin(string loginToken)
        {
            var meString = await _simpleHttpService.SendGet(loginToken, "http://www.reddit.com/api/me.json");
            return (!string.IsNullOrWhiteSpace(meString) && meString != "{}");
        }

    }
}
