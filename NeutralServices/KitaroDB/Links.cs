using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using KitaroDB;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.NeutralServices.KitaroDB
{
    class Links
    {
		private static string linksDatabase = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\links_v2.ism";

        private static Task<Links> _instanceTask;
        private static async Task<Links> GetInstanceImpl()
        {
			var db = await DB.CreateAsync(linksDatabase, DBCreateFlags.None, ushort.MaxValue - 100, new DBKey[]
            {
                new DBKey(16, 0, DBKeyFlags.Alpha, "main", false, false, false, 0),
                new DBKey(8, 16, DBKeyFlags.AutoTime, "creation_timestamp", false, false, false, 1),
                new DBKey(8, 24, DBKeyFlags.AutoSequence, "insertion_order", false, false, false, 2)
            });
            return new Links(db);
        }

        public static Task<Links> GetInstance()
        {
            if (_instanceTask == null)
            {
                _instanceTask = GetInstanceImpl();
            }
            return _instanceTask;
        }

        private Links(DB db)
        {
            _linksDB = db;
        }

        DB _linksDB;
        private static int LinkKeySpaceSize = 32;
        private static int PrimaryKeySpaceSize = 16;
        public async Task StoreLink(Thing link)
        {
            var value = JsonConvert.SerializeObject(link);
            var encodedValue = Encoding.UTF8.GetBytes(value);

            var combinedSpace = new byte[encodedValue.Length + LinkKeySpaceSize];
            var keySpace = new byte[PrimaryKeySpaceSize];

            //these ids are stored in base 36 so we will never see unicode chars
            for (int i = 0; i < 8 && i < ((Link)link.Data).SubredditId.Length; i++)
                keySpace[i] = combinedSpace[i] = (byte)((Link)link.Data).SubredditId[i];

            for (int i = 8; i < 16 && i < (byte)((Link)link.Data).Name.Length + 8; i++)
                keySpace[i] = combinedSpace[i] = (byte)((Link)link.Data).Name[i - 8];

            encodedValue.CopyTo(combinedSpace, LinkKeySpaceSize);

            var commentsCursor = await _linksDB.SeekAsync(_linksDB.GetKeys().First(), keySpace, DBReadFlags.AutoLock | DBReadFlags.WaitOnLock);
            if (commentsCursor != null)
            {
                using (commentsCursor)
                {
                    await commentsCursor.UpdateAsync(combinedSpace);
                }
            }
            else
                await _linksDB.InsertAsync(combinedSpace);
        }

        public async Task Clear()
        {
            _linksDB.Dispose();
            _linksDB = null;
			await DB.PurgeAsync(linksDatabase);

			_linksDB = await DB.CreateAsync(linksDatabase, DBCreateFlags.None, ushort.MaxValue - 100, new DBKey[]
            {
                new DBKey(16, 0, DBKeyFlags.Alpha, "main", false, false, false, 0),
                new DBKey(8, 16, DBKeyFlags.AutoTime, "creation_timestamp", false, false, false, 1),
                new DBKey(8, 24, DBKeyFlags.AutoSequence, "insertion_order", false, false, false, 2)
            });
        }

        public async Task StoreLinks(Listing listing)
        {
            foreach (var link in listing.Data.Children)
            {
                if (link.Data is Link)
                {
                    await StoreLink(link);
                }
            }
        }

        private async Task<Listing> DeserializeCursor(DBCursor cursor, int count)
        {
            var redditService = ServiceLocator.Current.GetInstance<IRedditService>();
            int i = 0;
            var targetListing = new Listing { Data = new ListingData { Children = new List<Thing>() } };

            if(cursor != null)
            {
                do
                {
                    var currentRecord = cursor.Get();
                    var decodedListing = Encoding.UTF8.GetString(currentRecord, LinkKeySpaceSize, currentRecord.Length - LinkKeySpaceSize);
                    var deserializedLink = JsonConvert.DeserializeObject<Thing>(decodedListing);
                    if (deserializedLink != null && deserializedLink.Data is Link)
                    {
                        redditService.AddFlairInfo(((Link)deserializedLink.Data).Name, ((Link)deserializedLink.Data).Author);
                    }
                    targetListing.Data.Children.Add(deserializedLink);
                    
                    if (i++ > count)
                    {
                        //after type encoding
                        targetListing.Data.After = Encoding.UTF8.GetString(currentRecord, 0, 16);
                    }

                }while(await cursor.MoveNextAsync());
            }

            return targetListing;
        }
        private async Task<string> TranslateSubredditNameToId(Subreddits subreddits, string subredditName)
        {
            var subreddit = await subreddits.GetSubreddit(null, subredditName);
            if (subreddit != null)
                return ((Subreddit)subreddit.Data).Name;
            else
                return null;
        }

        public async Task<Listing> LinksForSubreddit(Subreddits subreddits, string subredditName, string after)
        {
            var subredditId = await TranslateSubredditNameToId(subreddits, subredditName);
            if (subredditId == null)
                return new Listing { Data = new ListingData { Children = new List<Thing>() } };
            
            var keyspace = new byte[8];

            for (int i = 0; i < 8 && i < subredditId.Length; i++)
                keyspace[i] = (byte)subredditId[i];

            var linkCursor = await _linksDB.SelectAsync(_linksDB.GetKeys().First(), keyspace);

            if(after != null && linkCursor != null)
            {
                var afterKeyspace = new byte[16];

                for (int i = 0; i < 16 && i < after.Length + 10; i++)
                    afterKeyspace[i] = (byte)after[i + 2]; //skip ahead past the after type identifier

                await linkCursor.SeekAsync(_linksDB.GetKeys().First(), afterKeyspace, DBReadFlags.NoLock);
            }

            return await DeserializeCursor(linkCursor, 25);
        }

        public async Task<Listing> AllLinks(string after)
        {
            DBCursor linkCursor;

            if (after != null && after.Length > 0)
            {
                var afterKeyspace = new byte[16];

                for (int i = 0; i < 16 && i < after.Length; i++)
                    afterKeyspace[i] = (byte)after[i]; //skip ahead past the after type identifier

                linkCursor = await _linksDB.SeekAsync(_linksDB.GetKeys().First(), afterKeyspace, DBReadFlags.NoLock);
            }
            else
            {
                linkCursor = await _linksDB.SeekAsync(DBReadFlags.NoLock);
            }

            return await DeserializeCursor(linkCursor, 25);
        }
    }
}
