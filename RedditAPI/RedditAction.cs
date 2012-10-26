using KitaroDB;
using Newtonsoft.Json;
using Baconography.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace Baconography.RedditAPI
{
    public interface IRedditAction
    {
        void Run(User loggedInUser);
    }

    public interface IRedditActionQueue
    {
        void AddAction(IRedditAction action);
        Task Init();
    }

    public class RedditActionQueueService : IRedditActionQueue
    {
        public IUsersService _userService;
        private RedditActionQueue _redditActionQueue;
        public RedditActionQueueService(IUsersService userService)
        {
            _userService = userService;
        }

        public async void AddAction(IRedditAction action)
        {
            if (_redditActionQueue == null)
                await Init();

            _redditActionQueue.AddAction(action);
        }

        public async Task Init()
        {
            _redditActionQueue = await RedditActionQueue.RunActionQueue(_userService);
        }
    }

    public class RedditActionQueue
    {
        private IUsersService _userService;
        private DB _actionDB;
        private ThreadPoolTimer _queueTimer;

        public RedditActionQueue(DB actionDB, IUsersService userService)
        {
            _userService = userService;
            _actionDB = actionDB;
        }

        public static async Task<RedditActionQueue> RunActionQueue(IUsersService userService)
        {
            //tell the key value pair infrastructure to allow duplicates
            //we dont really have a key, all we actually wanted was an ordered queue
            //the duplicates mechanism should give us that.
            var awaitDB = await DB.CreateAsync(
                Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\RedditActionQueue.ism",
                DBCreateFlags.None,
                ushort.MaxValue - 100,
                new DBKey[] { new DBKey(8, 0, DBKeyFlags.KeyValue, "default", true, false, false, 0) });
            var queue = new RedditActionQueue(awaitDB, userService);
            
            //tick at the max rate allowed by reddit once every .5 seconds
            queue._queueTimer = ThreadPoolTimer.CreateTimer(queue.RunQueue, new TimeSpan(500));
            return queue;
        }

        public async void RunQueue(ThreadPoolTimer timer) 
        {
            try
            {
                var actionCursor = await _actionDB.SeekAsync(_actionDB.GetKeys().First(), "action", DBReadFlags.AutoLock);
                if (actionCursor != null)
                {
                    using (actionCursor)
                    {
                        try
                        {
                            //use the JSON convert mechanism for deserializing arbitrary types (having stored them in $type)
                            var deserializedAction = JsonConvert.DeserializeObject(actionCursor.GetString(),
                                new JsonSerializerSettings { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects }) as IRedditAction;
                            var user = await _userService.GetUser();
                            if (user != null && user.Me != null)
                            {
                                deserializedAction.Run(user);
                                await actionCursor.DeleteAsync();
                            }
                        }
                        catch (Exception)
                        {
                            //need to do some logging here, possibly retry depending on what kind of failure it was
                        }
                    }
                }
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            _queueTimer = ThreadPoolTimer.CreateTimer(RunQueue, new TimeSpan(1, 0, 1));
        }

        public async void AddAction(IRedditAction action)
        {
            //use the JSON convert mechanism for serializing arbitrary types (storing the typename in $type)
            await _actionDB.InsertAsync("action", JsonConvert.SerializeObject(action, new JsonSerializerSettings { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects }));
        }
    }
}
