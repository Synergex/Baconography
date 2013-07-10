using BaconographyPortable.Model.Reddit;
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

        public void Initialize(ISmartOfflineService smartOfflineService, ISuspensionService suspensionService, IRedditService redditService,
            ISettingsService settingsService, ISystemServices systemServices, IOfflineService offlineService, INotificationService notificationService,
            IUserService userService)
        {
            _smartOfflineService = smartOfflineService;
            _suspensionService = suspensionService;
            _redditService = redditService;
            _settingsService = settingsService;
            _systemServices = systemServices;
            _offlineService = offlineService;
            _notificationService = notificationService;
            _userService = userService;

            _smartOfflineService.OffliningOpportunity += _smartOfflineService_OffliningOpportunity;
        }

        Stack<Link> _linkThingsAwaitingOfflining = new Stack<Link>();
        bool _isOfflining = false;

        const int TopSubsetMaximum = 10;
        async void _smartOfflineService_OffliningOpportunity(OffliningOpportunityPriority priority, NetworkConnectivityStatus networkStatus, CancellationToken token)
        {
            //dont want to do this more then one at a time
            if (_isOfflining)
                return;

            _isOfflining = true;
            try
            {
                //good chance to try and process our delayed actions
                //we dont want to do to much if we've got actions to run and we're just in an idle state
                if (_smartOfflineService.IsActivityIdle)
                {
                    if (await RunPeriodic() || token.IsCancellationRequested)
                        return;
                }

                if (_linkThingsAwaitingOfflining.Count > 0)
                {
                    var linkThingToOffline = _linkThingsAwaitingOfflining.Pop();
                    //since it goes through the normal infrastructure it will get no-op'ed if we've already offlined it
                    //and it will get stored if we havent or if its too far out of date
                    await GetCommentsOnPost(linkThingToOffline.SubredditId, linkThingToOffline.Permalink, null);
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

                    var filteredLinks = newLinks.Where(p => targetDomains.Contains(p.Domain) || subs.Contains(p.SubredditId));
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

        public Task<Listing> Search(string query, int? limit, bool reddits)
        {
            //TODO cache this for reddit searches since those wont be likely to change
            return _redditService.Search(query, limit, reddits);
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

        private Listing MaybeStoreSubscribedSubredditListing(Listing listing, User user)
        {
            if (user != null && user.Username != null && listing != null && listing.Data.Children != null && listing.Data.Children.Count > 0)
            {
                _offlineService.StoreOrderedThings("user-sub:" + user.Username, listing.Data.Children);
            }
            return listing;
        }

        public async Task<HashSet<string>> GetSubscribedSubreddits()
        {
            var user = await _userService.GetUser();
            //this can change on a whim, but lets be frugal and only ask for it once per day
            IEnumerable<Thing> subscribedSubreddits = await _offlineService.RetrieveOrderedThings("user-sub:" + user.Username, TimeSpan.FromDays(1));
            if (subscribedSubreddits == null)
            {
                var subscribedSubredditsListing = MaybeStoreSubscribedSubredditListing(await _redditService.GetSubscribedSubredditListing(), user);
                if (subscribedSubredditsListing != null)
                    subscribedSubreddits = subscribedSubredditsListing.Data.Children;
            }

            if (subscribedSubreddits != null)
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
                return new HashSet<string>(subscribedSubreddits
                    .Select(hashifyListing)
                    .Where(str => str != null));
            }
            else
                return new HashSet<string>();
        }

        public async Task<Listing> GetSubscribedSubredditListing()
        {
            var user = await _userService.GetUser();
            //this can change on a whim, but lets be frugal and only ask for it once per day
            var subscribedSubreddits = await _offlineService.RetrieveOrderedThings("user-sub:" + user.Username, TimeSpan.FromDays(1));
            if (subscribedSubreddits == null)
                return MaybeStoreSubscribedSubredditListing(await _redditService.GetSubscribedSubredditListing(), user);
            else
                return new Listing { Data = new ListingData { Children = subscribedSubreddits.ToList() } };
        }

        public Task<Listing> GetDefaultSubreddits()
        {
            return _redditService.GetDefaultSubreddits();
        }

        private Listing MaybeStoreSubreddits(Listing listing)
        {
            if(listing.Data.Children.Count > 0)
                _offlineService.StoreOrderedThings("subreddits:", listing.Data.Children);
            return listing;
        }

        public async Task<Listing> GetSubreddits(int? limit)
        {
            var cachedSubreddits = await _offlineService.RetrieveOrderedThings("subreddits:", TimeSpan.FromDays(30));
            if (cachedSubreddits != null && cachedSubreddits.Count() > 0)
                return new Listing { Data = new ListingData { Children = cachedSubreddits.ToList() } };
            else
                return MaybeStoreSubreddits(await _redditService.GetSubreddits(limit));
        }

        public Task<TypedThing<Subreddit>> GetSubreddit(string name)
        {
            return _redditService.GetSubreddit(name);
        }

        public Task<Listing> GetPostsByUser(string username, int? limit)
        {
            return _redditService.GetPostsByUser(username, limit);
        }

        private Listing MaybeStorePostsBySubreddit(Listing listing)
        {
            _offlineService.StoreLinks(listing);
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
            var requestedLinkInfo = listing.Data.Children.FirstOrDefault(thing => thing.Data is Link);
            if (!_linkToOpMap.ContainsKey(((Link)requestedLinkInfo.Data).Name))
            {
                _linkToOpMap.Add(((Link)requestedLinkInfo.Data).Name, ((Link)requestedLinkInfo.Data).Author);
            }
            lock (_currentlyStoringComments)
            {
                if (_currentlyStoringComments.ContainsKey(permalink))
                    return listing;

                _currentlyStoringComments.Add(permalink, listing);
            }
            _offlineService.StoreComments(listing).ContinueWith(task =>
                {
                    lock(_currentlyStoringComments)
                    {
                        _currentlyStoringComments.Remove(permalink);
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
            var cachedLink = await _offlineService.RetrieveLinkByUrl(permalink, TimeSpan.FromDays(1));
            Thing linkThing = null;
            //make sure there are some comments otherwise its more expensive to make two calls then just the one
            if (cachedLink != null && cachedLink.TypedData.CommentCount > 15 && (linkThing = await GetLinkByUrl("http://www.reddit.com" + permalink)) != null)
            {
                //compare to see if there was any significant change
                var typedLink = new TypedThing<Link>(linkThing);
                var percentChange = Math.Abs((typedLink.TypedData.CommentCount - cachedLink.TypedData.CommentCount) / ((typedLink.TypedData.CommentCount + cachedLink.TypedData.CommentCount) / 2));
                if (percentChange > 5)
                    return MaybeStoreCommentsOnPost(await _redditService.GetCommentsOnPost(subreddit, permalink, limit), permalink);

                var comments = await _offlineService.GetTopLevelComments(cachedLink.TypedData.SubredditId, cachedLink.TypedData.Name, limit ?? 500);
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
            try
            {
                if (string.IsNullOrWhiteSpace(parentId) || content == null)
                    return;

                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await _redditService.AddComment(parentId, content);
                else
                    await _offlineService.EnqueueAction("AddComment", new Dictionary<string, string> { { "parentId", parentId }, { "content", content } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    _notificationService.CreateErrorNotification(ex);

                _offlineService.EnqueueAction("AddComment", new Dictionary<string, string> { { "parentId", parentId }, { "content", content } }).Start();
            }


        }

        public async Task EditComment(string thingId, string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(thingId) || text == null)
                    return;
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await _redditService.EditComment(thingId, text);
                else
                    await _offlineService.EnqueueAction("EditComment", new Dictionary<string, string> { { "thingId", thingId }, { "text", text } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    _notificationService.CreateErrorNotification(ex);

                _offlineService.EnqueueAction("EditComment", new Dictionary<string, string> { { "thingId", thingId }, { "text", text } }).Start();
            }


        }

        public async Task AddMessage(string recipient, string subject, string message)
        {
            try
            {
                if (recipient == null || subject == null || message == null)
                    return;

                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await _redditService.AddMessage(recipient, subject, message);
                else
                    await _offlineService.EnqueueAction("AddMessage", new Dictionary<string, string> { { "recipient", recipient }, { "subject", subject }, { "message", message } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    _notificationService.CreateErrorNotification(ex);

                _offlineService.EnqueueAction("AddMessage", new Dictionary<string, string> { { "recipient", recipient }, { "subject", subject }, { "message", message } }).Start();
            }
        }

        public async Task AddPost(string kind, string url, string subreddit, string title)
        {
            try
            {
                if (kind == null || url == null || subreddit == null || title == null)
                    return;

                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await _redditService.AddPost(kind, url, subreddit, title);
                else
                    await _offlineService.EnqueueAction("AddPost", new Dictionary<string, string> 
                    { 
                        { "kind", kind }, 
                        { "url", url }, 
                        { "subreddit", subreddit }, 
                        { "title", title } 
                    });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    _notificationService.CreateErrorNotification(ex);

                _offlineService.EnqueueAction("AddPost", new Dictionary<string, string> 
                    { 
                        { "kind", kind }, 
                        { "url", url }, 
                        { "subreddit", subreddit }, 
                        { "title", title } 
                    }).Start();
            }
        }

        public async Task AddVote(string thingId, int direction)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await _redditService.AddVote(thingId, direction);
                else
                    await _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "thingId", thingId }, { "direction", direction.ToString() } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    _notificationService.CreateErrorNotification(ex);

                _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "thingId", thingId }, { "direction", direction.ToString() } }).Start();
            }
        }

        public async Task AddSubredditSubscription(string subreddit, bool unsub)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await _redditService.AddSubredditSubscription(subreddit, unsub);
                else
                    await _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "subreddit", subreddit }, { "direcunsubtion", unsub.ToString() } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    _notificationService.CreateErrorNotification(ex);

                _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "subreddit", subreddit }, { "direcunsubtion", unsub.ToString() } }).Start();
            }
        }

        public async Task AddSavedThing(string thingId)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await _redditService.AddSavedThing(thingId);
                else
                    await _offlineService.EnqueueAction("AddSavedThing", new Dictionary<string, string> { { "thingId", thingId } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    _notificationService.CreateErrorNotification(ex);

                _offlineService.EnqueueAction("AddSavedThing", new Dictionary<string, string> { { "thingId", thingId } }).Start();
            }
        }

        public async Task AddReportOnThing(string thingId)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await _redditService.AddReportOnThing(thingId);
                else
                    await _offlineService.EnqueueAction("AddReportOnThing", new Dictionary<string, string> { { "thingId", thingId } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if(System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    _notificationService.CreateErrorNotification(ex);

                _offlineService.EnqueueAction("AddReportOnThing", new Dictionary<string, string> { { "thingId", thingId } }).Start();
            }
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
                                    await AddPost(actionTpl.Item2["kind"], actionTpl.Item2["url"], actionTpl.Item2["subreddit"], actionTpl.Item2["title"]);
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


        public Task<Listing> GetMessages(int? limit)
        {
            return _redditService.GetMessages(limit);
        }


        public Task SubmitCaptcha(string captcha)
        {
            return _redditService.SubmitCaptcha(captcha);
        }
    }
}
