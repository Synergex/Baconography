using Baconography.NeutralServices.KitaroDB;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
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
        public OfflineService(IRedditService redditService)
        {
            _redditService = redditService;
        }

        IRedditService _redditService;
        Task _instanceTask;
        bool _hasQueuedActions;

        private async Task InitializeImpl()
        {
            _comments = await Comments.GetInstance();
            _links = await Links.GetInstance();

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
        }

        public Task Initialize()
        {
            if (_instanceTask == null)
            {
                _instanceTask = InitializeImpl();
            }
            return _instanceTask;
        }

        Comments _comments;
        Links _links;
        DB _settingsDb;
        DB _historyDb;
        DB _actionsDb;

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
            await _links.StoreLinks(listing);
        }

        public async Task<Listing> LinksForSubreddit(string subredditName, string after)
        {
            await Initialize();
            return await _links.LinksForSubreddit(_redditService, subredditName, after);
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
            await _historyDb.InsertAsync(link, "");
        }

        public async Task ClearHistory()
        {
            await Initialize();
            _historyDb.Dispose();
            _historyDb = null;
            _historyDb = await DB.CreateAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\history.ism", DBCreateFlags.Supersede);
        }

        public async Task<bool> HasHistory(string link)
        {
            await Initialize();

            return await _historyDb.GetAsync(link) != null;
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
    }

}