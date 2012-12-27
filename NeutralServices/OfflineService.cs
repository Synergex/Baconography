using Baconography.NeutralServices.KitaroDB;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using KitaroDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Baconography.NeutralServices
{
    class OfflineService : IOfflineService
    {
        public OfflineService(IRedditService redditService, INotificationService notificationService, ISettingsService settingsService)
        {
            _redditService = redditService;
            _notificationService = notificationService;
            _settingsService = settingsService;
        }

        INotificationService _notificationService;
        IRedditService _redditService;
        ISettingsService _settingsService;
        Task _instanceTask;
        bool _hasQueuedActions;

        private async Task InitializeImpl()
        {
            _comments = await Comments.GetInstance();
            _links = await Links.GetInstance();
            _subreddits = await Subreddits.GetInstance();

            //tell the key value pair infrastructure to allow duplicates
            //we dont really have a key, all we actually wanted was an ordered queue
            //the duplicates mechanism should give us that
            _actionsDb = await DB.CreateAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\actions.ism", DBCreateFlags.None,
                ushort.MaxValue - 100,
                new DBKey[] { new DBKey(8, 0, DBKeyFlags.KeyValue, "default", true, false, false, 0) });

            _historyDb = await DB.CreateAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\history.ism", DBCreateFlags.None);
            _settingsDb = await DB.CreateAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\settings.ism", DBCreateFlags.None);

            //get our initial action queue state
            var actionCursor = await _actionsDb.SeekAsync(_actionsDb.GetKeys().First(), "action", DBReadFlags.AutoLock);
            _hasQueuedActions = actionCursor != null;

            var historyCursor = await _historyDb.SeekAsync(DBReadFlags.NoLock);
            if (historyCursor != null)
            {
                using (historyCursor)
                {
                    do
                    {
                        _clickHistory.Add(historyCursor.GetString());
                    } while (await historyCursor.MoveNextAsync());
                }
            }
        }

        public Task Initialize()
        {
            lock (this)
            {
                if (_instanceTask == null)
                {
                    _instanceTask = InitializeImpl();
                }
            }
            return _instanceTask;
        }

        Comments _comments;
        Links _links;
        Subreddits _subreddits;
        DB _settingsDb;
        DB _historyDb;
        DB _actionsDb;
        DB _thumbnailsDb;
        HashSet<string> _clickHistory = new HashSet<string>();

        public async Task Clear()
        {
            await Initialize();
            await _comments.Clear();
            await _links.Clear();
        }

        public async Task<IEnumerable<Task<Tuple<string, byte[]>>>> GetImages(string uri)
        {
            await Initialize();
            return Enumerable.Empty<Task<Tuple<string, byte[]>>>();
        }

        public async Task StoreComments(Listing listing)
        {
            await Initialize();
            await _comments.StoreComments(listing);
        }

        public async Task<Listing> GetTopLevelComments(string subredditId, string linkId, int count)
        {
            await Initialize();
            return await _comments.GetTopLevelComments(subredditId, linkId, count);
        }

        public async Task<Listing> GetMoreComments(string subredditId, string linkId, IEnumerable<string> ids)
        {
            await Initialize();
            return await _comments.GetMoreComments(subredditId, linkId, ids);
        }

        public async Task StoreLink(Thing link)
        {
            await Initialize();
            await _links.StoreLink(link);
        }

        public async Task StoreLinks(Listing listing)
        {
            await Initialize();

            try
            {
                await _links.StoreLinks(listing);

                foreach (var link in listing.Data.Children)
                {
                    if (link.Data is Link)
                    {
                        await _subreddits.StoreSubreddit(((Link)link.Data).SubredditId, ((Link)link.Data).Subreddit);
                        Messenger.Default.Send<OfflineStatusMessage>(new OfflineStatusMessage { LinkId = ((Link)link.Data).Id, Status = OfflineStatusMessage.OfflineStatus.Initial });
                    }
                }


                _notificationService.CreateKitaroDBNotification(string.Format("{0} Links now available offline", listing.Data.Children.Count));
                //this is where we should kick off the non reddit content getter/converter on a seperate thread

                var remainingMoreThings = new List<Tuple<Link, TypedThing<More>>>();


                foreach (var link in listing.Data.Children)
                {
                    bool finishedLink = true;
                    var linkData = link.Data as Link;
                    if (linkData != null)
                    {
                        var comments = await _redditService.GetCommentsOnPost(linkData.Subreddit, linkData.Permalink, null);
                        if (comments != null)
                        {
                            if (comments.Data.Children.Count == 0)
                            {
                                throw new Exception();
                            }
                            await (await Comments.GetInstance()).StoreComments(comments);
                            var moreChild = comments.Data.Children.LastOrDefault(comment => comment.Data is More);
                            if (moreChild != null)
                            {
                                TypedThing<More> moreThing = new TypedThing<More>(moreChild);
                                if (moreThing != null && moreThing.Data.Children.Count > 0)
                                {
                                    if (moreThing.Data.Children.Count > _settingsService.MaxTopLevelOfflineComments)
                                    {
                                        moreThing.Data.Children.RemoveRange(_settingsService.MaxTopLevelOfflineComments, moreThing.Data.Children.Count - _settingsService.MaxTopLevelOfflineComments - 1);
                                    }
                                    finishedLink = false;
                                    remainingMoreThings.Add(Tuple.Create(linkData, moreThing));
                                }
                            }
                        }
                    }
                    Messenger.Default.Send<OfflineStatusMessage>(new OfflineStatusMessage { LinkId = linkData.Id, Status = finishedLink ? OfflineStatusMessage.OfflineStatus.AllComments : OfflineStatusMessage.OfflineStatus.TopComments });
                }

                _notificationService.CreateKitaroDBNotification("Inital comments for offline links now available");

                //we've seperated getting the links and initial comments because we want to prioritize getting some data for all of the links instead of all the data for a very small number of links
                //ex, someone getting on a plane in 5 minutes wants to get what they can on a broad a selection of links as possible, rather than all of the comments on the latest 10 bazilion comment psy ama

                if (!_settingsService.OfflineOnlyGetsFirstSet)
                {

                    uint commentCount = 0;
                    foreach (var moreThingTpl in remainingMoreThings)
                    {
                        var moreThing = moreThingTpl.Item2;
                        var linkData = moreThingTpl.Item1;

                        while (moreThing != null && moreThing.Data.Children.Count > 0)
                        {
                            var moreChildren = moreThing.Data.Children.Take(500).ToList();
                            var moreComments = await _redditService.GetMoreOnListing(moreChildren, linkData.Name, linkData.Subreddit);
                            var moreMoreComments = moreComments.Data.Children.FirstOrDefault(thing => thing.Data is More);
                            if (moreMoreComments != null)
                            {
                                //we asked for more then reddit was willing to give us back
                                //just make sure we dont lose anyone
                                moreChildren.RemoveAll((str) => ((More)moreMoreComments.Data).Children.Contains(str));
                                //all thats left is what was returned so remove them by value from the moreThing
                                moreThing.Data.Children.RemoveAll((str) => moreChildren.Contains(str));
                                commentCount += (uint)((More)moreMoreComments.Data).Children.Count;
                            }
                            else
                            {
                                moreThing.Data.Children.RemoveRange(0, moreChildren.Count);
                            }
                            await (await Comments.GetInstance()).StoreComments(moreComments);
                        }
                        Messenger.Default.Send<OfflineStatusMessage>(new OfflineStatusMessage { LinkId = linkData.Id, Status = OfflineStatusMessage.OfflineStatus.AllComments });

                    }
                    _notificationService.CreateKitaroDBNotification(string.Format("{0} Top level comments for offline links now available", commentCount));
                }
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
            }
        }

        public async Task<Listing> LinksForSubreddit(string subredditName, string after)
        {
            await Initialize();
            return await _links.LinksForSubreddit(_subreddits, subredditName, after);
        }

        public async Task<Listing> AllLinks(string after)
        {
            await Initialize();
            return await _links.AllLinks(after);
        }

        public async Task StoreOrderedThings(string key, IEnumerable<Thing> things)
        {
            await Initialize();

        }

        public async Task<IEnumerable<Thing>> RetrieveOrderedThings(string key)
        {
            await Initialize();
            return null;
        }

        public async Task StoreOrderedThings(IListingProvider listingProvider)
        {
            await Initialize();
        }

        public async Task StoreSetting(string name, string value)
        {
            await Initialize();

            await _settingsDb.InsertAsync(name, value);
        }

        public async Task<string> GetSetting(string name)
        {
            await Initialize();

            return await _settingsDb.GetAsync(name);
        }


        public async Task StoreHistory(string link)
        {
            await Initialize();
            if (!_clickHistory.Contains(link))
            {
                _clickHistory.Add(link);
                await _historyDb.InsertAsync(link, link);
            }
            
        }

        public async Task ClearHistory()
        {
            await Initialize();
            _clickHistory.Clear();
            _historyDb.Dispose();
            _historyDb = null;
            _historyDb = await DB.CreateAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\history.ism", DBCreateFlags.Supersede);
        }

        public bool HasHistory(string link)
        {
            return _clickHistory.Contains(link);
        }

        public async Task Suspend()
        {
            await Initialize();
        }

        public async Task EnqueueAction(string actionName, Dictionary<string, string> parameters)
        {
            await Initialize();
            _hasQueuedActions = true;
            await _actionsDb.InsertAsync("action", JsonConvert.SerializeObject(new { Name = actionName, Parameters = parameters }));
        }

        public async Task<Tuple<string, Dictionary<string, string>>> DequeueAction()
        {
            await Initialize();

            if (_hasQueuedActions)
            {
                var actionCursor = await _actionsDb.SeekAsync(_actionsDb.GetKeys().First(), "action", DBReadFlags.AutoLock);
                if (actionCursor != null)
                {
                    using (actionCursor)
                    {
                        var tpl = JsonConvert.DeserializeAnonymousType(actionCursor.GetString(), new { Name = "", Parameters = new Dictionary<string, string>() });
                        return Tuple.Create(tpl.Name, tpl.Parameters);
                    }
                }
                _hasQueuedActions = false;
            }
            return null;
        }


        public Task<Thing> GetSubreddit(string name)
        {
            return _subreddits.GetSubreddit(null, name);
        }
    }

}