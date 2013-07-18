﻿using KitaroDB;
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

        private static Task<Comments> _instanceTask;
        CancellationTokenSource _terminateSource = new CancellationTokenSource();
        private static async Task<Comments> GetInstanceImpl()
        {
            return new Comments(await GetDBInstance());
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

        public async Task Clear()
        {
            _commentsDB.Dispose();
            _commentsDB = null;
			await DB.PurgeAsync(commentsDatabase);
            _commentsDB = await GetDBInstance();
        }

#if WINDOWS_PHONE
        SHA1 permalinkDigest = new SHA1Managed();
#else
        HashAlgorithmProvider permalinkDigest = HashAlgorithmProvider.OpenAlgorithm("SHA1");
#endif

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
                var recordBytes = new byte[compressedBytes.Length + 28];
#if WINDOWS_PHONE
                var keyBytes = permalinkDigest.ComputeHash(Encoding.UTF8.GetBytes(((Link)linkThing.Data).Permalink));
#else
                var keyBytes = permalinkDigest.HashData(Encoding.UTF8.GetBytes(((Link)linkThing.Data).Permalink).AsBuffer()).ToArray();
#endif
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
            }
            catch (Exception ex)
            {
                var errorText = DBError.TranslateError((uint)ex.HResult);
                //throw new Exception(errorText);
                Debug.WriteLine(errorText);
                Debug.WriteLine(ex.ToString());
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


        internal void Terminate()
        {
            _terminateSource.Cancel();
        }
    }
}
