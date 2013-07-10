using KitaroDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaconographyPortable.Model.Reddit;
using System.Diagnostics;

namespace Baconography.NeutralServices.KitaroDB
{
    class Comments
    {
		private static string commentsDatabase = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\comments-v3.ism";

        private static Task<Comments> _instanceTask;
        private static async Task<Comments> GetInstanceImpl()
        {
            return new Comments(await GetDBInstance());
        }

        private static async Task<DB> GetDBInstance()
        {
			var db = await DB.CreateAsync(commentsDatabase, DBCreateFlags.None, 0, new DBKey[]
            {
                new DBKey(12, 0, DBKeyFlags.Alpha, "link_id", false, false, false, 0),
                new DBKey(8, 12, DBKeyFlags.AutoTime, "creation_timestamp", false, false, false, 1)
            });
            return db;
        }

        public static Task<Comments> GetInstance()
        {
            lock (typeof(Comments))
            {
                if (_instanceTask == null)
                {
                    _instanceTask = GetInstanceImpl();
                }
            }
            return _instanceTask;
        }

        private Comments(DB db)
        {
            _commentsDB = db;
        }

        DB _commentsDB;

        private byte[] GenerateCombinedKeyspace(string linkId, byte[] value)
        {
            var keyspace = new byte[12 + value.Length];

            //these ids are stored in base 36 so we will never see unicode chars
            for (int i = 0; i < 8 && i < linkId.Length; i++)
                keyspace[i] = (byte)linkId[i];

            value.CopyTo(keyspace, 12);

            return keyspace;
        }

        //public async Task StoreComment(Thing thing, string subredditId, string linkId, string parentId, string name)
        //{
        //    ((Comment)thing.Data).BodyHtml = ""; //we dont need this and on large comments this causes problems for the max record size

        //    var replies = ((Comment)thing.Data).Replies;
        //    //try to keep down the number of async operations we have to do when reloading
        //    if(replies != null && replies.Data.Children.Count > 2)
        //        ((Comment)thing.Data).Replies = null;

        //    var value = JsonConvert.SerializeObject(thing);
        //    var encodedValue = Encoding.UTF8.GetBytes(value);

        //    var keyspace = GenerateDirectKeyspace(subredditId, linkId, name);
        //    var combinedSpace = GenerateCombinedKeyspace(subredditId, linkId, parentId, name, encodedValue);

        //    var commentsCursor = await _commentsDB.SeekAsync(_commentsDB.GetKeys()[1], keyspace, DBReadFlags.AutoLock | DBReadFlags.WaitOnLock);
        //    if (commentsCursor != null)
        //    {
        //        using (commentsCursor)
        //        {
        //            await commentsCursor.UpdateAsync(combinedSpace);
        //        }
        //    }
        //    else
        //    {
        //        try
        //        {
        //            await _commentsDB.InsertAsync(combinedSpace);
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //    }
        //    if(replies != null && replies.Data.Children.Count > 0)
        //        await StoreComments(replies);
        //}

        public async Task Clear()
        {
            _commentsDB.Dispose();
            _commentsDB = null;
			await DB.PurgeAsync(commentsDatabase);
            _commentsDB = await GetDBInstance();
        }

        public async Task StoreComments(Listing listing)
        {
            try
            {
                var linkThing = listing.Data.Children.First();
                if (!(linkThing.Data is Link))
                    return;

                string key = ((Link)linkThing.Data).Name;
                

                var compressor = new BaconographyPortable.Model.Compression.CompressionService();
                var compressedBytes = compressor.Compress(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(listing)));
                var recordBytes = new byte[compressedBytes.Length + 20];
                var keyBytes = new byte[12];
                Array.Copy(compressedBytes, 0, recordBytes, 20, compressedBytes.Length);
                //the 12 bytes not written here will be filled with the current time stamp by kdb
                //these ids are stored in base 36 so we will never see unicode chars
                for (int i = 0; i < 12 && i < key.Length; i++)
                    keyBytes[i] = recordBytes[i] = (byte)key[i];

                using (var blobCursor = await _commentsDB.SeekAsync(_commentsDB.GetKeys()[0], keyBytes, DBReadFlags.WaitOnLock))
                {
                    if (blobCursor != null)
                    {
                        await blobCursor.UpdateAsync(recordBytes);
                    }
                    else
                    {
                        await _commentsDB.InsertAsync(recordBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                var errorText = DBError.TranslateError((uint)ex.HResult);
                //throw new Exception(errorText);
                Debug.WriteLine(errorText);
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task<Listing> GetTopLevelComments(string subredditId, string linkId, int count)
        {
            var keyBytes = new byte[12];
            for (int i = 0; i < 12 && i < linkId.Length; i++)
                keyBytes[i] = (byte)linkId[i];
            bool badElement = false;
            try
            {
                using (var blobCursor = await _commentsDB.SeekAsync(_commentsDB.GetKeys()[0], keyBytes, DBReadFlags.WaitOnLock))
                {
                    if (blobCursor != null)
                    {
                        var gottenBlob = blobCursor.Get();
                        var compressor = new BaconographyPortable.Model.Compression.CompressionService();
                        var decompressedBytes = compressor.Decompress(gottenBlob, 20);
                        var result = JsonConvert.DeserializeObject<Listing>(Encoding.UTF8.GetString(decompressedBytes, 0, decompressedBytes.Length));
                        return result;
                    }
                }
            }
            catch
            {
                badElement = true;
            }

            if (badElement)
            {
                try
                {
                    await _commentsDB.DeleteAsync(keyBytes);
                }
                catch
                {
                }
            }
            return null;
            
        }

    }
}
