using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaconographyPortable.Services.Impl
{
    public class SmartOfflineRedditService : IRedditService
    {
        ISmartOfflineService _smartOfflineService;
        ISuspensionService _suspensionService;
        IRedditService _redditService;
        ISettingsService _settingsService;
        ISystemServices _systemServices;
        IOfflineService _offlineService;
        INotificationService _notificationService;
        IUserService _userService;
        ISuspendableWorkQueue _suspendableWorkQueue;

        public void Initialize(ISmartOfflineService smartOfflineService, ISuspensionService suspensionService, IRedditService redditService,
            ISettingsService settingsService, ISystemServices systemServices, IOfflineService offlineService, INotificationService notificationService,
            IUserService userService, ISuspendableWorkQueue suspendableWorkQueue)
        {
            _smartOfflineService = smartOfflineService;
            _suspensionService = suspensionService;
            _redditService = redditService;
            _settingsService = settingsService;
            _systemServices = systemServices;
            _offlineService = offlineService;
            _notificationService = notificationService;
            _userService = userService;
            _suspendableWorkQueue = suspendableWorkQueue;

            _smartOfflineService.OffliningOpportunity += _smartOfflineService_OffliningOpportunity;
            Messenger.Default.Register<UserLoggedInMessage>(this, UserLoggedIn);
        }

        private void UserLoggedIn(UserLoggedInMessage obj)
        {
            _subscribedSubredditListing = null;
            _subscribedSubreddits = null;
        }

        Stack<Link> _linkThingsAwaitingOfflining = new Stack<Link>();
        HashSet<string> _recentlyLoadedComments = new HashSet<string>();
        bool _isOfflining = false;

        DateTime _lastMeCheck = DateTime.MinValue;
        DateTime _nextOffliningTime = DateTime.MinValue;

        const int TopSubsetMaximum = 10;

        void OfflineComments(string subredditId, string permalink)
        {
            _suspendableWorkQueue.QueueLowImportanceRestartableWork(async (token2) =>
            {
                //since it goes through the normal infrastructure it will get no-op'ed if we've already offlined it
                //and it will get stored if we havent or if its too far out of date
                await GetCommentsOnPost(subredditId, permalink, null);
            });
        }

        async void _smartOfflineService_OffliningOpportunity(OffliningOpportunityPriority priority, NetworkConnectivityStatus networkStatus, CancellationToken token)
        {
            if (!_settingsService.AllowPredictiveOfflining || (DateTime.Now - _nextOffliningTime).TotalMinutes < 10)
                return;

            try
            {
                //good chance to try and process our delayed actions
                //we dont want to do to much if we've got actions to run and we're just in an idle state
                if (_smartOfflineService.IsActivityIdle)
                {
                    if (await RunPeriodic() || token.IsCancellationRequested)
                        return;
                }

                var user = await _userService.GetUser();
                if (user != null && (DateTime.Now - _lastMeCheck) >= TimeSpan.FromMinutes(10))
                {
                    _lastMeCheck = DateTime.Now;
                    user.Me = await GetMe(user);
                }

                if ((user != null && user.Me != null && user.Me.HasMail)
                    || !(await _offlineService.UserHasOfflineMessages(user)))
                {
                    await GetMessages(null);
                }

                if (_linkThingsAwaitingOfflining.Count > 0)
                {
                    while (!token.IsCancellationRequested && _linkThingsAwaitingOfflining.Count > 0)
                    {
                        var linkThingToOffline = _linkThingsAwaitingOfflining.Pop();
                        if (_recentlyLoadedComments.Contains(linkThingToOffline.Permalink))
                        {
                            _recentlyLoadedComments.Add(linkThingToOffline.Permalink);
                            OfflineComments(linkThingToOffline.SubredditId, linkThingToOffline.Permalink);
                        }
                    }
                }
                else
                {
                    HashSet<string> targetDomains = new HashSet<string>();
                    var newLinks = _smartOfflineService.OfflineableLinkThingsFromContext.Select(thing => thing.TypedData).ToList();

                    var domainAggs = await _offlineService.GetDomainAggregates(TopSubsetMaximum, 25);
                    var subredditAggs = await _offlineService.GetSubredditAggregates(TopSubsetMaximum, 25);

                    var linkTotal = subredditAggs.Sum(p => p.LinkClicks);
                    var commentTotal = subredditAggs.Sum(p => p.CommentClicks);
                    var hashes = domainAggs.Select(p => p.DomainHash);
                    var subs = subredditAggs.Select(p => p.SubredditId);

                    foreach (var domain in newLinks.Select(p => p.Domain).Distinct())
                    {
                        var hash = (uint)domain.GetHashCode();
                        if (hashes.Contains(hash))
                            targetDomains.Add(domain);
                    }

                    var filteredLinks = newLinks.Where(p => targetDomains.Contains(p.Domain) || subs.Contains(p.SubredditId)).ToList();
                    if (filteredLinks.Count < 5)
                    {
                        filteredLinks = newLinks;
                    }

                    if (filteredLinks.All(link => _recentlyLoadedComments.Contains(link.Permalink)))
                    {
                        _nextOffliningTime = DateTime.Now.AddMinutes(10);
                    }

                    _linkThingsAwaitingOfflining = new Stack<Link>(filteredLinks);
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                _isOfflining = false;
            }
        }

        public async Task<Account> GetMe()
        {
            var currentUser = await _userService.GetUser();
            if (string.IsNullOrEmpty(currentUser.Username))
                return null;
            else
                return await GetMe(currentUser);
        }

        public async Task<Account> GetMe(User user)
        {
            if (_settingsService.IsOnline())
            {
                return await _redditService.GetMe();
            }
            else
            {
                var thing = await _offlineService.RetrieveThing(string.Format("account-user:{0}", user.Username), TimeSpan.FromDays(1024));
                return thing != null ? thing.Data as Account : null;
            }
        }

        public Task<bool> CheckLogin(string loginToken)
        {
            return _redditService.CheckLogin(loginToken);
        }

        public Task<User> Login(string username, string password)
        {
            return _redditService.Login(username, password);
        }

        public Task<Listing> Search(string query, int? limit, bool reddits, string restrictedToSubreddit)
        {
            //TODO cache this for reddit searches since those wont be likely to change
            return _redditService.Search(query, limit, reddits, restrictedToSubreddit);
        }

        private Thing MaybeStoreThing(Thing thing)
        {
            //TODO: this might need to be implemented unsure how much benifit there is here though
            return thing;
        }

        public async Task<Thing> GetThingById(string id)
        {
            //determine the type of the id (t1, t2, t3, t4)
            //go check what we've got stored of that type for a matching id
            if (id.Length > 2)
            {
                var idCode = id.Substring(0, 2);
                Thing gottenThing = null;
                switch (idCode)
                {
                    //case "t1":
                        //gottenThing = await _offlineService.RetrieveComment(id);
                        //break;
                    case "t3":
                        gottenThing = await _offlineService.RetrieveLink(id);
                        break;
                    case "t5":
                        gottenThing = await _offlineService.RetrieveSubredditById(id);
                        break;
                }

                if (gottenThing == null)
                    return MaybeStoreThing(await _redditService.GetThingById(id));
                else
                    return gottenThing;
            }
            return null;
        }

        private async Task MaybeStoreSubscribedSubredditListing(Listing listing, User user)
        {
            try
            {
                if (user != null && user.Username != null && listing != null && listing.Data.Children != null && listing.Data.Children.Count > 0)
                {
                    await _offlineService.StoreOrderedThings("sublist:" + user.Username, listing.Data.Children);
                }
            }
            catch { }
        }

        private async Task MaybeStoredSubredditListing(Listing listing)
        {
            try
            {
                if (listing != null && listing.Data.Children != null && listing.Data.Children.Count > 0)
                {
                    await _offlineService.StoreOrderedThings("reddits:", listing.Data.Children);
                }
            }
            catch { }
        }

        Listing _subscribedSubredditListing;
        Listing _subredditListing;
        HashSet<string> _subscribedSubreddits;
        public async Task<HashSet<string>> GetSubscribedSubreddits()
        {
            if (_subscribedSubreddits != null)
                return _subscribedSubreddits;

            var result = await GetSubscribedSubredditListing();
            if (result != null && result.Data.Children.Count > 0)
            {
                _subscribedSubreddits = ThingUtility.HashifyListing(result.Data.Children);
            }
            else
            {
                _subscribedSubreddits = ThingUtility.HashifyListing((await GetDefaultSubreddits()).Data.Children);
            }
            return _subscribedSubreddits;
        }

        
        public async Task<Listing> GetSubscribedSubredditListing()
        {
            if (_subscribedSubredditListing != null)
                return _subscribedSubredditListing;
            
            var result = await _redditService.GetSubscribedSubredditListing();
            if (result != null && result.Data.Children.Count > 0)
            {
                _subscribedSubredditListing = result;
            }
            else
            {
                _subscribedSubredditListing = await GetDefaultSubreddits();
            }

            await MaybeStoreSubscribedSubredditListing(result, await _userService.GetUser());

            return _subscribedSubredditListing;
        }

        public Task<Listing> GetDefaultSubreddits()
        {
            return _redditService.GetDefaultSubreddits();
        }

        public async Task<Listing> GetSubreddits(int? limit)
        {
            if (_subredditListing != null)
                return _subredditListing;

            var result = await _redditService.GetSubreddits(limit);
            if (result != null && result.Data.Children.Count > 0)
            {
                _subredditListing = result;
                await MaybeStoredSubredditListing(result);
            }
            else
            {
                _subredditListing = await GetDefaultSubreddits();
            }

            return _subredditListing;
        }

        private async void UpdateCachedSubreddit(string name)
        {
            try
            {
                var result = await _redditService.GetSubreddit(name);
            
                await _offlineService.StoreSubreddit(result);
            }
            catch { }
        }

        public async Task<TypedThing<Subreddit>> GetSubreddit(string name)
        {
            try
            {
                var thing = await _offlineService.GetSubreddit(name);
                if (thing != null && thing.Data is Subreddit && !string.IsNullOrEmpty(((Subreddit)thing.Data).Description))
                {
                    UpdateCachedSubreddit(name);
                    return new TypedThing<Subreddit>(thing);
                }
                else
                {
                    var result = await _redditService.GetSubreddit(name);
                    await _offlineService.StoreSubreddit(result);
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        public Task<Listing> GetPostsByUser(string username, int? limit)
        {
            return _redditService.GetPostsByUser(username, limit);
        }

        List<Task> activeMaybeTasks = new List<Task>();
        private Listing MaybeStorePostsBySubreddit(Listing listing)
        {
            if (listing == null)
                return null;
            _suspendableWorkQueue.QueueLowImportanceRestartableWork(async (token) =>
                {
                    Task maybeTask = null;
                    try
                    {
                        maybeTask = _offlineService.StoreLinks(listing);
                        activeMaybeTasks.Add(maybeTask);
                        await maybeTask;
                    }
                    catch { }
                    finally
                    {
                        if (maybeTask != null)
                        {
                            lock (activeMaybeTasks)
                            {
                                activeMaybeTasks.Remove(maybeTask);
                            }
                        }
                    }
                });
            
            return listing;
        }

        public async Task<Listing> GetPostsBySubreddit(string subreddit, int? limit)
        {
            //we dont need to serve cached versions of this, we just want to serve it if the network has failed us
            return MaybeStorePostsBySubreddit(await _redditService.GetPostsBySubreddit(subreddit, limit));
        }

        public Task<Listing> GetMoreOnListing(IEnumerable<string> childrenIds, string contentId, string subreddit)
        {
            return _redditService.GetMoreOnListing(childrenIds, contentId, subreddit);
        }

        Dictionary<string, Listing> _currentlyStoringComments = new Dictionary<string, Listing>();
        private Listing MaybeStoreCommentsOnPost(Listing listing, string permalink)
        {
            if (listing == null)
                return null;

            var requestedLinkInfo = listing.Data.Children.FirstOrDefault(thing => thing.Data is Link);
            if (requestedLinkInfo == null)
                return listing;

            if (!_linkToOpMap.ContainsKey(((Link)requestedLinkInfo.Data).Name))
            {
                _linkToOpMap.Add(((Link)requestedLinkInfo.Data).Name, ((Link)requestedLinkInfo.Data).Author);
            }
            _suspendableWorkQueue.QueueLowImportanceRestartableWork(async (token) =>
                {
                    lock (_currentlyStoringComments)
                    {
                        if (_currentlyStoringComments.ContainsKey(permalink))
                            return;

                        _currentlyStoringComments.Add(permalink, listing);
                    }
                    try
                    {
                        await _offlineService.StoreComments(listing);
                    }
                    catch { }
                    finally
                    {
                        lock (_currentlyStoringComments)
                        {
                            _currentlyStoringComments.Remove(permalink);
                        }
                    }
                });
            return listing;
        }

        public async Task<Listing> GetCommentsOnPost(string subreddit, string permalink, int? limit)
        {
            lock (_currentlyStoringComments)
            {
                if (_currentlyStoringComments.ContainsKey(permalink))
                    return _currentlyStoringComments[permalink];
            }

            var cachedPermalink = permalink;
            if (permalink.EndsWith(".json?sort=hot"))
                cachedPermalink = permalink.Replace(".json?sort=hot", "");

            var cachedLink = await _offlineService.RetrieveLinkByUrl(cachedPermalink, TimeSpan.FromDays(1));
            var commentMetadata = await _offlineService.GetCommentMetadata(cachedPermalink);
            //make sure there are some comments otherwise its more expensive to make two calls then just the one
            if (cachedLink != null && commentMetadata.Item1 != 0)
            {
                if (commentMetadata.Item1 != cachedLink.TypedData.CommentCount || _invalidatedIds.Contains(cachedLink.Data.Name))
                    return MaybeStoreCommentsOnPost(await _redditService.GetCommentsOnPost(subreddit, permalink, limit), permalink);

                var comments = await _offlineService.GetTopLevelComments(cachedPermalink, limit ?? 500);
                if (comments != null && comments.Data.Children.Count > 0)
                    return comments;
                else
                    return MaybeStoreCommentsOnPost(await _redditService.GetCommentsOnPost(subreddit, permalink, limit), permalink);
            }
            else
                return MaybeStoreCommentsOnPost(await _redditService.GetCommentsOnPost(subreddit, permalink, limit), permalink);
        }

        public async Task<Thing> GetLinkByUrl(string url)
        {
            string permaLink = url;
            if (url.StartsWith("http://www.reddit.com"))
                permaLink = url.Substring("http://www.reddit.com".Length);

            var cachedLink = await _offlineService.RetrieveLinkByUrl(permaLink, TimeSpan.FromMinutes(10));
            if (cachedLink != null)
                return cachedLink;
            else
                return await _redditService.GetLinkByUrl(url);
        }

        public Task<Listing> GetAdditionalFromListing(string baseUrl, string after, int? limit)
        {
            //TODO: persist these for retrival when in actual offline mode
            return _redditService.GetAdditionalFromListing(baseUrl, after, limit);
        }

        public Task<TypedThing<Account>> GetAccountInfo(string accountName)
        {
            return _redditService.GetAccountInfo(accountName);
        }

        Dictionary<string, string> _linkToOpMap = new Dictionary<string, string>();
        Dictionary<string, HashSet<string>> _subredditToModMap = new Dictionary<string, HashSet<string>>();

        public AuthorFlairKind GetUsernameModifiers(string username, string linkid, string subreddit)
        {
            var initialResult = _redditService.GetUsernameModifiers(username, linkid, subreddit);
            if (initialResult != AuthorFlairKind.None)
                return initialResult;


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

        public async Task AddComment(string parentId, string content)
        {
            if (string.IsNullOrWhiteSpace(parentId) || content == null)
                return;

            if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                await _redditService.AddComment(parentId, content);
            else
                await _offlineService.EnqueueAction("AddComment", new Dictionary<string, string> { { "parentId", parentId }, { "content", content } });
        }

        public HashSet<string> _invalidatedIds = new HashSet<string>();

        public async Task EditComment(string thingId, string text)
        {
            if (string.IsNullOrWhiteSpace(thingId) || text == null)
                return;

            _invalidatedIds.Add(thingId);

            if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                await _redditService.EditComment(thingId, text);
            else
                await _offlineService.EnqueueAction("EditComment", new Dictionary<string, string> { { "thingId", thingId }, { "text", text } });

        }

        public async Task AddMessage(string recipient, string subject, string message)
        {
            if (recipient == null || subject == null || message == null)
                return;

            if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                await _redditService.AddMessage(recipient, subject, message);
            else
                await _offlineService.EnqueueAction("AddMessage", new Dictionary<string, string> { { "recipient", recipient }, { "subject", subject }, { "message", message } });
            
        }

        public async Task AddPost(string kind, string url, string text, string subreddit, string title)
        {
            if (kind == null || url == null || text == null || subreddit == null || title == null)
                return;

            if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                await _redditService.AddPost(kind, url, text, subreddit, title);
            else
                await _offlineService.EnqueueAction("AddPost", new Dictionary<string, string> 
                { 
                    { "kind", kind }, 
                    { "url", url },
                    { "text", text},
                    { "subreddit", subreddit }, 
                    { "title", title } 
                });
        }

        public async Task EditPost(string text, string name)
        {
            if (text == null || name == null)
                return;

            _invalidatedIds.Add(name);

            if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                await _redditService.EditPost(text, name);
            else
                await _offlineService.EnqueueAction("EditPost", new Dictionary<string, string> 
                { 
                    {"text", text},
                    {"thing_id", name}
                });
        }

        public async Task AddVote(string thingId, int direction)
        {
            if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                await _redditService.AddVote(thingId, direction);
            else
                await _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "thingId", thingId }, { "direction", direction.ToString() } });
        }

        public async Task AddSubredditSubscription(string subreddit, bool unsub)
        {
            if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                await _redditService.AddSubredditSubscription(subreddit, unsub);
            else
                await _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "subreddit", subreddit }, { "direcunsubtion", unsub.ToString() } });
        }

        public async Task AddSavedThing(string thingId)
        {
            if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                await _redditService.AddSavedThing(thingId);
            else
                await _offlineService.EnqueueAction("AddSavedThing", new Dictionary<string, string> { { "thingId", thingId } });
        }

        public async Task AddReportOnThing(string thingId)
        {
            if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                await _redditService.AddReportOnThing(thingId);
            else
                await _offlineService.EnqueueAction("AddReportOnThing", new Dictionary<string, string> { { "thingId", thingId } });
        }

        //we dont need to be particularly active here, as we dont want to burn battery when nothing is happening and we dont want to choke out
        //the content requests when the user is actively browsing around
        public async Task<bool> RunPeriodic()
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                {
                    var actionTpl = await _offlineService.DequeueAction();
                    try
                    {
                        if (actionTpl != null)
                        {
                            switch (actionTpl.Item1)
                            {
                                case "AddComment":
                                    {
                                        await AddComment(actionTpl.Item2["parentId"], actionTpl.Item2["content"]);
                                        break;
                                    }
                                case "EditComment":
                                    {
                                        await EditComment(actionTpl.Item2["thingId"], actionTpl.Item2["text"]);
                                        break;
                                    }
                                case "AddMessage":
                                    {
                                        await AddMessage(actionTpl.Item2["recipient"], actionTpl.Item2["subject"], actionTpl.Item2["message"]);
                                        break;
                                    }
                                case "AddPost":
                                    {
                                        await AddPost(actionTpl.Item2["kind"], actionTpl.Item2["url"], actionTpl.Item2["text"], actionTpl.Item2["subreddit"], actionTpl.Item2["title"]);
                                        break;
                                    }
                                case "EditPost":
                                    {
                                        await EditPost(actionTpl.Item2["text"], actionTpl.Item2["name"]);
                                        break;
                                    }
                                case "AddVote":
                                    {
                                        await AddVote(actionTpl.Item2["thingId"], int.Parse(actionTpl.Item2["direction"]));
                                        break;
                                    }
                                case "AddSubredditSubscription":
                                    {
                                        await AddSubredditSubscription(actionTpl.Item2["subreddit"], bool.Parse(actionTpl.Item2["direction"]));
                                        break;
                                    }
                                case "AddSavedThing":
                                    {
                                        await AddSavedThing(actionTpl.Item2["thingId"]);
                                        break;
                                    }
                                case "AddReportOnThing":
                                    {
                                        await AddReportOnThing(actionTpl.Item2["thingId"]);
                                        break;
                                    }
                                default:
                                    return false;
                            }
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        //fall through to the enqueue
                    }
                    await _offlineService.EnqueueAction(actionTpl.Item1, actionTpl.Item2);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            return false;
        }


        public void AddFlairInfo(string linkId, string opName)
        {
            _redditService.AddFlairInfo(linkId, opName);
        }

        Dictionary<string, Listing> _currentlyStoringMessages = new Dictionary<string, Listing>();
        private Listing MaybeStoreMessages(User user, Listing listing)
        {
            if (user == null || string.IsNullOrEmpty(user.Username))
                return listing;

            lock (_currentlyStoringMessages)
            {
                if (_currentlyStoringMessages.ContainsKey(user.Username))
                    return listing;

                _currentlyStoringMessages.Add(user.Username, listing);
            }
            _offlineService.StoreMessages(user, listing).ContinueWith(task =>
            {
                lock (_currentlyStoringMessages)
                {
                    _currentlyStoringMessages.Remove(user.Username);
                }
            });
            return listing;
        }

        public async Task<Listing> GetMessages(int? limit)
        {
            var user = await _userService.GetUser();
            if (user == null || string.IsNullOrEmpty(user.Username))
            {
                return await _redditService.GetMessages(limit);
            }

            lock (_currentlyStoringMessages)
            {
                if (_currentlyStoringMessages.ContainsKey(user.Username))
                    return _currentlyStoringMessages[user.Username];
            }

            if (user.Username != null)
                return MaybeStoreMessages(user, await _redditService.GetMessages(limit));

            var messages = await _offlineService.GetMessages(user);
            if (messages != null && messages.Data.Children.Count > 0)
                return messages;
            else
                return MaybeStoreMessages(user, await _redditService.GetMessages(limit));
        }

        public Task SubmitCaptcha(string captcha)
        {
            return _redditService.SubmitCaptcha(captcha);
        }

        public Task ReadMessage(string id)
        {
            return _redditService.ReadMessage(id);
        }
    }
}
