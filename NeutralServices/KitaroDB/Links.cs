using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using KitaroDB;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Baconography.NeutralServices.KitaroDB
{
    class Links
    {
		private static string linksDatabase = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\links_v3.ism";

        CancellationTokenSource _terminateSource = new CancellationTokenSource();
        private static Task<Links> _instanceTask;
        private static async Task<DB> CreateDB()
        {
            return await DB.CreateAsync(linksDatabase, DBCreateFlags.None, 0, new DBKey[]
                {
                    new DBKey(20, 0, DBKeyFlags.Alpha, "main", false, false, false, 0),
                    new DBKey(8, 8, DBKeyFlags.Alpha, "directid", true, false, false, 1),
                    new DBKey(4, 16, DBKeyFlags.Alpha, "urlhash", true, false, false, 2),
                    new DBKey(8, 20, DBKeyFlags.AutoTime, "creation_timestamp", false, false, false, 3),
                    new DBKey(8, 28, DBKeyFlags.AutoSequence, "insertion_order", false, false, false, 4),
                
                });
        }
        private static async Task<Links> GetInstanceImpl()
        {
            try
            {
                var db = await CreateDB();
                return new Links(db);
            }
            catch (Exception ex)
            {
                var errorText = DBError.TranslateError((uint)ex.HResult);
                throw;
            }
        }

        public static Task<Links> GetInstance()
        {
            if (_instanceTask == null)
            {
                lock (typeof(Links))
                {
                    if (_instanceTask == null)
                    {
                        _instanceTask = GetInstanceImpl();
                    }
                }
            }
            return _instanceTask;
        }

        private Links(DB db)
        {
            _linksDB = db;
        }

        DB _linksDB;
        private static int LinkKeySpaceSize = 36;
        private static int PrimaryKeySpaceSize = 20;
        public async Task StoreLink(Thing link)
        {
            try
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

                var hashBytes = BitConverter.GetBytes(((Link)link.Data).Permalink.GetHashCode());
                hashBytes.CopyTo(combinedSpace, 16);
                hashBytes.CopyTo(keySpace, 16);
                encodedValue.CopyTo(combinedSpace, LinkKeySpaceSize);

                using (var commentsCursor = await _linksDB.SeekAsync(_linksDB.GetKeys().First(), keySpace, DBReadFlags.AutoLock | DBReadFlags.WaitOnLock))
                {
                    if (_terminateSource.IsCancellationRequested)
                        return;
                    if (commentsCursor != null)
                        await commentsCursor.UpdateAsync(combinedSpace);

                    else
                        await _linksDB.InsertAsync(combinedSpace);
                }
            }
            catch (Exception ex)
            {
                var errorCode = DBError.TranslateError((uint)ex.HResult);
                Debug.WriteLine(errorCode);
            }
        }

        public async Task Clear()
        {
            _linksDB.Dispose();
            _linksDB = null;
			await DB.PurgeAsync(linksDatabase);

            _linksDB = await CreateDB();
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
            try
            {
                var subredditId = await TranslateSubredditNameToId(subreddits, subredditName);
                if (subredditId == null)
                    return new Listing { Data = new ListingData { Children = new List<Thing>() } };

                var keyspace = new byte[8];

                for (int i = 0; i < 8 && i < subredditId.Length; i++)
                    keyspace[i] = (byte)subredditId[i];

                using (var linkCursor = await _linksDB.SelectAsync(_linksDB.GetKeys().First(), keyspace))
                {
                    if (after != null && linkCursor != null)
                    {
                        var afterKeyspace = new byte[16];

                        for (int i = 0; i < 16 && i < after.Length + 10; i++)
                            afterKeyspace[i] = (byte)after[i + 2]; //skip ahead past the after type identifier

                        await linkCursor.SeekAsync(_linksDB.GetKeys().First(), afterKeyspace, DBReadFlags.NoLock);
                    }

                    return await DeserializeCursor(linkCursor, 25);
                }
            }
            catch (Exception ex)
            {
                var errorCode = DBError.TranslateError((uint)ex.HResult);
                Debug.WriteLine(errorCode);
                return new Listing { Data = new ListingData { Children = new List<Thing>() } };
            }
        }

        public async Task<Listing> AllLinks(string after)
        {
            DBCursor linkCursor = null;
            try
            {
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
            catch (Exception ex)
            {
                var errorCode = DBError.TranslateError((uint)ex.HResult);
                Debug.WriteLine(errorCode);
                return new Listing { Data = new ListingData { Children = new List<Thing>() } };
            }
            finally
            {
                if (linkCursor != null)
                    linkCursor.Dispose();
            }
        }

        public async Task<TypedThing<Link>> GetLink(string url, string id, TimeSpan maxAge)
        {
            DBCursor linkCursor = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var urlKeyspace = BitConverter.GetBytes(url.GetHashCode());
                    linkCursor = await _linksDB.SeekAsync(_linksDB.GetKeys()[2], urlKeyspace, DBReadFlags.NoLock);
                }
                else if (!string.IsNullOrWhiteSpace(id))
                {
                    var idKeyspace = new byte[16];

                    for (int i = 0; i < 8 && i < id.Length; i++)
                        idKeyspace[i] = (byte)id[i];

                    linkCursor = await _linksDB.SeekAsync(_linksDB.GetKeys()[1], idKeyspace, DBReadFlags.NoLock);
                }

                if (linkCursor != null)
                {
                    var gottenBlob = linkCursor.Get();
                    var microseconds = BitConverter.ToInt64(gottenBlob, 20);
                    var updatedTime = new DateTime(microseconds * 10).AddYears(1969);
                    var blobAge = DateTime.Now - updatedTime;
                    if (blobAge < maxAge)
                    {

                        var listing = await DeserializeCursor(linkCursor, 1);
                        var thing = listing.Data.Children.FirstOrDefault();
                        if (thing != null && thing.Data is Link)
                            return new TypedThing<Link>(thing);
                        else
                            return null;
                    }
                    else
                        return null;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                var errorCode = DBError.TranslateError((uint)ex.HResult);
                Debug.WriteLine(errorCode);
                throw;
            }
            finally
            {
                if (linkCursor != null)
                    linkCursor.Dispose();
            }
        }

        internal void Terminate()
        {
            _terminateSource.Cancel();
        }
    }
}
