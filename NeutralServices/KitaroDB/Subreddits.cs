using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using KitaroDB;
using Newtonsoft.Json;
using System.Threading;

namespace Baconography.NeutralServices.KitaroDB
{
    class Subreddits
    {
		private static string subredditsDatabase = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\subreddits_v2.ism";

        CancellationTokenSource _terminateSource = new CancellationTokenSource();
        private static Task<Subreddits> _instanceTask;
        private static async Task<Subreddits> GetInstanceImpl()
        {
            var db = await DB.CreateAsync(subredditsDatabase, DBCreateFlags.None, ushort.MaxValue - 100, new DBKey[]
            {
                new DBKey(24, 0, DBKeyFlags.Alpha, "name", false, false, false, 0),
                new DBKey(12, 24, DBKeyFlags.Alpha, "id", false, false, false, 1)
            });
            return new Subreddits(db);
        }

        public static Task<Subreddits> GetInstance()
        {
            if (_instanceTask == null)
            {
                lock (typeof(Subreddits))
                {
                    if (_instanceTask == null)
                    {
                        _instanceTask = GetInstanceImpl();
                    }
                }
            }
            return _instanceTask;
        }

        private Subreddits(DB db)
        {
            _subredditsDB = db;
        }

        DB _subredditsDB;

        const int NameKeySpaceSize = 24;
        const int IdKeySpaceSize = 12;
        const int SubredditKeySpaceSize = 36;

        private byte[] GenerateNameKeyspace(string name)
        {
            var keyspace = new byte[NameKeySpaceSize];

            for (int i = 0; i < 24 && i < name.Length; i++)
                keyspace[i] = (byte)name[i];

            return keyspace;
        }

        private byte[] GenerateIdKeyspace(string id)
        {
            var keyspace = new byte[IdKeySpaceSize];

            for (int i = 0; i < 12 && i < id.Length; i++)
                keyspace[i] = (byte)id[i];

            return keyspace;
        }

        private byte[] GenerateCombinedKeyspace(string name, string id, byte[] value)
        {
            var keyspace = new byte[SubredditKeySpaceSize + value.Length];

            for (int i = 0; i < 24 && i < name.Length; i++)
                keyspace[i] = (byte)name[i];

            for (int i = 0; i < 12 && i < id.Length; i++)
                keyspace[i + 24] = (byte)id[i];


            value.CopyTo(keyspace, SubredditKeySpaceSize);

            return keyspace;
        }

        public Task StoreSubreddit(string id, string name)
        {
            var partialThing = new Thing { Kind = "t5", Data = new Subreddit { DisplayName = name, Name = id } };
            return StoreSubreddit(partialThing);
        }

        public async Task StoreSubreddit(Thing thing)
        {
            var value = JsonConvert.SerializeObject(thing);
            var encodedValue = Encoding.UTF8.GetBytes(value);

            var keyspace = GenerateNameKeyspace(((Subreddit)thing.Data).DisplayName);
            var combinedSpace = GenerateCombinedKeyspace(((Subreddit)thing.Data).DisplayName, ((Subreddit)thing.Data).Name, encodedValue);

            using (var subredditsCursor = await _subredditsDB.SeekAsync(_subredditsDB.GetKeys()[0], keyspace, DBReadFlags.AutoLock | DBReadFlags.WaitOnLock))
            {
                if (_terminateSource.IsCancellationRequested)
                    return;

                if (subredditsCursor != null)
                {
                    if (((Subreddit)thing.Data).Description != null)
                        await subredditsCursor.UpdateAsync(combinedSpace);
                }
                else
                {
                    try
                    {
                        await _subredditsDB.InsertAsync(combinedSpace);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        public async Task<Thing> GetSubreddit(string id = null, string name = null)
        {
            try
            {
                byte[] keyspace = null;
                DBKey targetKey = null;
                if (id != null)
                {
                    keyspace = GenerateIdKeyspace(id);
                    targetKey = _subredditsDB.GetKeys()[1];
                }
                else if (name != null)
                {
                    keyspace = GenerateNameKeyspace(name);
                    targetKey = _subredditsDB.GetKeys()[0];
                }
                else
                    throw new ArgumentNullException("id/name");

                using (var subredditCursor = await _subredditsDB.SeekAsync(targetKey, keyspace, DBReadFlags.NoLock))
                {
                    if (subredditCursor != null)
                    {
                        var currentRecord = subredditCursor.Get();
                        var decodedListing = Encoding.UTF8.GetString(currentRecord, SubredditKeySpaceSize, currentRecord.Length - SubredditKeySpaceSize);
                        var deserializedComment = JsonConvert.DeserializeObject<Thing>(decodedListing);
                        return deserializedComment;

                    }
                    else
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        internal void Terminate()
        {
            _terminateSource.Cancel();
        }
    }
}
