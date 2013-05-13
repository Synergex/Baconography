using KitaroDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaconographyPortable.Model.Reddit;

namespace Baconography.NeutralServices.KitaroDB
{
    class Comments
    {
        private static Task<Comments> _instanceTask;
        private static async Task<Comments> GetInstanceImpl()
        {
            return new Comments(await GetDBInstance());
        }

        private static async Task<DB> GetDBInstance()
        {
            var dbLocation = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\comments-v2.ism";
            var db = await DB.CreateAsync(dbLocation, DBCreateFlags.None, ushort.MaxValue - 100, new DBKey[]
            {
                new DBKey(32, 0, DBKeyFlags.Alpha, "main", true, false, false, 0),
                new DBKey(20, 0, DBKeyFlags.Alpha, "direct", false, false, false, 1, new DBKeySegment[] { new DBKeySegment(12, 32, DBKeyFlags.Alpha, false) }), 
                new DBKey(8, 44, DBKeyFlags.AutoTime, "creation_timestamp", true, false, false, 2)
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
        private static int CommentKeySpaceSize = 52;
        private static int PrimaryKeySpaceSize = 44;
        private static int MainKeySpaceSize = 32;
        private static int DirectKeySpaceSize = 32;

        private byte[] GenerateMainKeyspace(string subredditId, string linkId, string parentId)
        {
            var keyspace = new byte[MainKeySpaceSize];

            //these ids are stored in base 36 so we will never see unicode chars
            for (int i = 0; i < 8 && i < subredditId.Length; i++)
                keyspace[i] = (byte)subredditId[i];

            for (int i = 0; i < 12 && i < linkId.Length; i++)
                keyspace[i + 8] = (byte)linkId[i];

            for (int i = 0; i < 12 && i < parentId.Length; i++)
                keyspace[i + 20] = (byte)parentId[i];

            return keyspace;
        }

        private byte[] GenerateCombinedKeyspace(string subredditId, string linkId, string parentId, string name, byte[] value)
        {
            var keyspace = new byte[CommentKeySpaceSize + value.Length];

            //these ids are stored in base 36 so we will never see unicode chars
            for (int i = 0; i < 8 && i < subredditId.Length; i++)
                keyspace[i] = (byte)subredditId[i];

            for (int i = 0; i < 12 && i < linkId.Length; i++)
                keyspace[i + 8] = (byte)linkId[i];

            for (int i = 0; i < 12 && i < parentId.Length; i++)
                keyspace[i + 20] = (byte)parentId[i];

            for (int i = 0; i < 12 && i < name.Length; i++)
                keyspace[i + 32] = (byte)name[i];

            value.CopyTo(keyspace, CommentKeySpaceSize);

            return keyspace;
        }

        private byte[] GenerateDirectKeyspace(string subredditId, string linkId, string name)
        {
            var keyspace = new byte[DirectKeySpaceSize];

            //these ids are stored in base 36 so we will never see unicode chars
            for (int i = 0; i < 8 && i < subredditId.Length; i++)
                keyspace[i] = (byte)subredditId[i];

            for (int i = 0; i < 12 && i < linkId.Length; i++)
                keyspace[i + 8] = (byte)linkId[i];

            for (int i = 0; i < 12 && i < name.Length; i++)
                keyspace[i + 20] = (byte)name[i];

            return keyspace;
        }

        public async Task StoreComment(Thing thing, string subredditId, string linkId, string parentId, string name)
        {
            ((Comment)thing.Data).BodyHtml = ""; //we dont need this and on large comments this causes problems for the max record size

            var replies = ((Comment)thing.Data).Replies;
            ((Comment)thing.Data).Replies = null;

            var value = JsonConvert.SerializeObject(thing);
            var encodedValue = Encoding.UTF8.GetBytes(value);

            var keyspace = GenerateDirectKeyspace(subredditId, linkId, name);
            var combinedSpace = GenerateCombinedKeyspace(subredditId, linkId, parentId, name, encodedValue);

            if (combinedSpace.Length > 65435)
                return; //failure until we get the record size bumped up next version

            var commentsCursor = await _commentsDB.SeekAsync(_commentsDB.GetKeys()[1], keyspace, DBReadFlags.AutoLock);
            if (commentsCursor != null)
            {
                using (commentsCursor)
                {
                    await commentsCursor.UpdateAsync(combinedSpace);
                }
            }
            else
            {
                try
                {
                    await _commentsDB.InsertAsync(combinedSpace);
                }
                catch (Exception ex)
                {
                }
            }
            if(replies != null)
                await StoreComments(replies);
        }

        public async Task Clear()
        {
            _commentsDB.Dispose();
            _commentsDB = null;
            await DB.PurgeAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "//comments-rev1.ism");
            _commentsDB = await GetDBInstance();
        }

        public async Task StoreComments(Listing listing)
        {
            Comment mostRecentComment = null;
            foreach (var comment in listing.Data.Children)
            {
                if (comment.Data is Comment)
                {
                    mostRecentComment = ((Comment)comment.Data);
                    await StoreComment(comment, mostRecentComment.SubredditId, mostRecentComment.LinkId, mostRecentComment.ParentId, mostRecentComment.Name);
                }
                //else if (comment.Data is More && mostRecentComment != null)
                //{
                //    await StoreComment(comment, mostRecentComment.SubredditId, mostRecentComment.LinkId, mostRecentComment.ParentId, "more");
                //}
            }
        }

        private async Task<Listing> DeserializeCursor(DBCursor cursor, int count, Listing existing = null)
        {
            var targetListing = existing ?? new Listing { Data = new ListingData { Children = new List<Thing>() } };
            int i = 0;
            if (cursor != null)
            {
                do
                {
                    var currentRecord = cursor.Get();
                    var decodedListing = Encoding.UTF8.GetString(currentRecord, CommentKeySpaceSize, currentRecord.Length - CommentKeySpaceSize);
                    var deserializedComment = JsonConvert.DeserializeObject<Thing>(decodedListing);
                    targetListing.Data.Children.Add(deserializedComment);
                    if (count == -1)
                    {
                        break;
                    }
                } while (await cursor.MoveNextAsync());
            }

            return targetListing;
        }

        private async Task<Listing> GetChildren(string subredditId, string linkId, string parentId)
        {
            var keyspace = GenerateMainKeyspace(subredditId, linkId, parentId);

            var linkCursor = await _commentsDB.SelectAsync(_commentsDB.GetKeys().First(), keyspace);
            Listing children = null;
            using(linkCursor)
            {
                //big enough to get everything
                children = await DeserializeCursor(linkCursor, 1000);
            }
            await FillInChildren(children);
            return children;
        }

        private async Task FillInChildren(Listing target)
        {
            foreach (var child in target.Data.Children)
            {
                var typedChild = child.Data as Comment;
                if(typedChild != null)
                    typedChild.Replies = await GetChildren(typedChild.SubredditId, typedChild.LinkId, typedChild.Name);
            }
        }

        public async Task<Listing> GetTopLevelComments(string subredditId, string linkId, int count)
        {
            var keyspace = GenerateMainKeyspace(subredditId, linkId, linkId);

            var commentCursor = await _commentsDB.SelectAsync(_commentsDB.GetKeys().First(), keyspace);
            Listing topLevelChildren = null;
            using (commentCursor)
            {
                topLevelChildren = await DeserializeCursor(commentCursor, count);
            }
            await FillInChildren(topLevelChildren);
            //we've got the order the wrong way this is a cheap fix for now
            topLevelChildren.Data.Children.Reverse();
            return topLevelChildren;
            
        }

        public async Task<Listing> GetMoreComments(string subredditId, string linkId, IEnumerable<string> ids)
        {
            var targetListing = new Listing { Data = new ListingData { Children = new List<Thing>() } };
            DBCursor moreCursor = null;
            try
            {
                foreach (var id in ids)
                {
                    var keyspace = GenerateDirectKeyspace(subredditId, linkId, id);
                    moreCursor = await _commentsDB.SeekAsync(_commentsDB.GetKeys()[1], keyspace, DBReadFlags.NoLock);

                    await DeserializeCursor(moreCursor, -1, targetListing);

                    await FillInChildren(targetListing);
                }
            }
            finally
            {
                if (moreCursor != null)
                    moreCursor.Dispose();
            }
            return targetListing;
        }
    }
}
