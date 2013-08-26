using KitaroDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaconographyPortable.Model.Reddit;
using System.Diagnostics;
#if WINDOWS_PHONE
using System.Security.Cryptography;
#else
using Windows.Security.Cryptography.Core;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

using System.Threading;

namespace Baconography.NeutralServices.KitaroDB
{
    class Comments
    {
		private static string commentsDatabase = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\comments-v3.ism";
        private static string commentsMetaDatabase = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\comments-meta-v2.ism";

        private static Task<Comments> _instanceTask;
        CancellationTokenSource _terminateSource = new CancellationTokenSource();
        private static async Task<Comments> GetInstanceImpl()
        {
            return new Comments(await GetDBInstance(), await GetMetaDBInstance());
        }

        private static async Task<DB> GetDBInstance()
        {
			var db = await DB.CreateAsync(commentsDatabase, DBCreateFlags.None, 0, new DBKey[]
            {
                new DBKey(20, 0, DBKeyFlags.Alpha, "permalinkhash", false, false, false, 0),
                new DBKey(8, 20, DBKeyFlags.AutoTime, "creation_timestamp", false, false, false, 1)
            });
            return db;
        }

        private static async Task<DB> GetMetaDBInstance()
        {
            var db = await DB.CreateAsync(commentsMetaDatabase, DBCreateFlags.None, 36, new DBKey[]
            {
                new DBKey(20, 0, DBKeyFlags.Alpha, "permalinkhash", false, false, false, 0),
                new DBKey(8, 20, DBKeyFlags.AutoTime, "creation_timestamp", false, false, false, 1)
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

        

        private Comments(DB db, DB metaDB)
        {
            _commentsDB = db;
            _metaDB = metaDB;
        }

        internal DB _commentsDB;
        internal DB _metaDB;

        public async Task Clear()
        {
            _commentsDB.Dispose();
            _commentsDB = null;
			await DB.PurgeAsync(commentsDatabase);
            _commentsDB = await GetDBInstance();

            _metaDB.Dispose();
            _metaDB = null;
            await DB.PurgeAsync(commentsMetaDatabase);
            _metaDB = await GetMetaDBInstance();
        }

#if WINDOWS_PHONE
        SHA1 permalinkDigest = new SHA1Managed();
#else
        HashAlgorithmProvider permalinkDigest = HashAlgorithmProvider.OpenAlgorithm("SHA1");
#endif

        private void StripCommentData(List<Thing> things)
        {
            if (things == null)
                return;

            foreach (var thing in things)
            {
                if (thing.Data is Comment)
                {
                    ((Comment)thing.Data).BodyHtml = "";
                    if(((Comment)thing.Data).Replies != null)
                        StripCommentData(((Comment)thing.Data).Replies.Data.Children);
                }
            }
        }

        public async Task StoreComments(Listing listing)
        {
            try
            {
                var linkThing = listing.Data.Children.First();
                if (!(linkThing.Data is Link))
                    return;


                var permalink = ((Link)linkThing.Data).Permalink;
                if (permalink.EndsWith(".json?sort=hot"))
                    permalink = permalink.Replace(".json?sort=hot", "");
#if WINDOWS_PHONE
                var keyBytes = permalinkDigest.ComputeHash(Encoding.UTF8.GetBytes(permalink));
#else
                var keyBytes = permalinkDigest.HashData(Encoding.UTF8.GetBytes(permalink).AsBuffer()).ToArray();
#endif

                //we can cut down on IO by about 50% by stripping out the HTML bodies of comments since we dont have any need for them
                StripCommentData(listing.Data.Children);

                string key = ((Link)linkThing.Data).Name;
                

                var compressor = new BaconographyPortable.Model.Compression.CompressionService();
                var compressedBytes = compressor.Compress(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(listing)));
                var recordBytes = new byte[compressedBytes.Length + 28];

                Array.Copy(compressedBytes, 0, recordBytes, 28, compressedBytes.Length);
                Array.Copy(keyBytes, 0, recordBytes, 0, keyBytes.Length);

                if (_terminateSource.IsCancellationRequested)
                    return;
                using (var blobCursor = await _commentsDB.SeekAsync(_commentsDB.GetKeys()[0], keyBytes, DBReadFlags.WaitOnLock))
                {
                    if (_terminateSource.IsCancellationRequested)
                        return;
                    if (blobCursor != null)
                    {
                        await blobCursor.UpdateAsync(recordBytes);
                    }
                    else
                    {
                        await _commentsDB.InsertAsync(recordBytes);
                    }
                }

                await StoreCommentMetadata(keyBytes, ((Link)linkThing.Data).CommentCount, listing.Data.Children.Count);
            }
            catch (Exception ex)
            {
                var errorText = DBError.TranslateError((uint)ex.HResult);
                //throw new Exception(errorText);
                Debug.WriteLine(errorText);
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task<Tuple<int, int>> GetCommentMetadata(string permalink)
        {
            if (permalink.EndsWith(".json?sort=hot"))
                permalink = permalink.Replace(".json?sort=hot", "");
#if WINDOWS_PHONE
            var keyBytes = permalinkDigest.ComputeHash(Encoding.UTF8.GetBytes(permalink));
#else
            var keyBytes = permalinkDigest.HashData(Encoding.UTF8.GetBytes(permalink).AsBuffer()).ToArray();
#endif

            using (var blobCursor = await _metaDB.SeekAsync(_metaDB.GetKeys()[0], keyBytes, DBReadFlags.WaitOnLock))
            {
                if (blobCursor != null)
                {
                    var bytes = blobCursor.Get();
                    return Tuple.Create(BitConverter.ToInt32(bytes, 28), BitConverter.ToInt32(bytes, 32));
                }
                else
                {
                    return Tuple.Create(0, 0);
                }
            }
        }

        private async Task StoreCommentMetadata(byte[] keyBytes, int linkComments, int actualComments)
        {
            var recordBytes = new byte[36];
            keyBytes.CopyTo(recordBytes, 0);
            BitConverter.GetBytes(linkComments).CopyTo(recordBytes, 28);
            BitConverter.GetBytes(actualComments).CopyTo(recordBytes, 32);
            using (var blobCursor = await _metaDB.SeekAsync(_metaDB.GetKeys()[0], keyBytes, DBReadFlags.WaitOnLock))
            {
                if (blobCursor != null)
                {
                    await blobCursor.UpdateAsync(recordBytes);
                }
                else
                {
                    try
                    {
                        await _metaDB.InsertAsync(recordBytes);
                    }
                    catch { } //someone beat us to it, just move on
                }
            }
        }

        public async Task<Listing> GetTopLevelComments(string permalink, int count)
        {
#if WINDOWS_PHONE
            var keyBytes = permalinkDigest.ComputeHash(Encoding.UTF8.GetBytes(permalink));
#else
            var keyBytes = permalinkDigest.HashData(Encoding.UTF8.GetBytes(permalink).AsBuffer()).ToArray();
#endif
            bool badElement = false;
            try
            {
                using (var blobCursor = await _commentsDB.SeekAsync(_commentsDB.GetKeys()[0], keyBytes, DBReadFlags.WaitOnLock))
                {
                    if (blobCursor != null)
                    {
                        var gottenBlob = blobCursor.Get();
                        var compressor = new BaconographyPortable.Model.Compression.CompressionService();
                        var decompressedBytes = compressor.Decompress(gottenBlob, 28);
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

        internal void Resume()
        {
            _terminateSource = new CancellationTokenSource();
        }

        internal void Terminate()
        {
            _terminateSource.Cancel();
        }
    }
}
