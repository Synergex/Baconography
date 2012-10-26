using KitaroDB;
using Newtonsoft.Json;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.OfflineStore
{
    class Comments
    {
        private static Task<Comments> _instanceTask;
        private static async Task<Comments> GetInstanceImpl()
        {
            var dbLocation = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\comments.ism";
            var db = await DB.CreateAsync(dbLocation, DBCreateFlags.None, ushort.MaxValue - 100, new DBKey[]
            {
                new DBKey(42, 0, DBKeyFlags.Alpha, "main", false, false, false, 0),
                new DBKey(8, 42, DBKeyFlags.AutoTime, "creation_timestamp", false, false, false, 1)
            });
            return new Comments(db);
        }

        public static Task<Comments> GetInstance()
        {
            if (_instanceTask == null)
            {
                _instanceTask = GetInstanceImpl();
            }
            return _instanceTask;
        }

        private Comments(DB db)
        {
            _commentsDB = db;
        }

        DB _commentsDB;
        private static int CommentKeySpaceSize = 50;
        private static int PrimaryKeySpaceSize = 42;
        public async Task StoreComment(Thing comment)
        {
            ((Comment)comment.Data).BodyHtml = ""; //we dont need this and on large comments this causes problems for the max record size
            var value = JsonConvert.SerializeObject(comment);
            var encodedValue = Encoding.UTF8.GetBytes(value);

            var combinedSpace = new byte[encodedValue.Length + CommentKeySpaceSize];
            var keyspace = new byte[PrimaryKeySpaceSize];
            var commentData = ((Comment)comment.Data);

            //these ids are stored in base 36 so we will never see unicode chars
            for (int i = 0; i < 8 && i < commentData.SubredditId.Length; i++)
                keyspace[i] = combinedSpace[i] = (byte)commentData.SubredditId[i];

            for (int i = 0; i < 12 && i < commentData.LinkId.Length; i++)
                keyspace[i + 8] = combinedSpace[i + 8] = (byte)commentData.LinkId[i];

            for (int i = 0; i < 12 && i < commentData.ParentId.Length; i++)
                keyspace[i + 20] = combinedSpace[i + 20] = (byte)commentData.ParentId[i];
            

            for (int i = 0; i < 12 && i < commentData.Name.Length; i++)
                keyspace[i + 32] = combinedSpace[i + 32] = (byte)commentData.Name[i];

            encodedValue.CopyTo(combinedSpace, CommentKeySpaceSize);

            if (combinedSpace.Length > 65435)
                return; //failure until we get the record size bumped up next version

            var commentsCursor = await _commentsDB.SeekAsync(_commentsDB.GetKeys().First(), keyspace, DBReadFlags.AutoLock);
            if (commentsCursor != null)
            {
                using (commentsCursor)
                {
                    await commentsCursor.UpdateAsync(combinedSpace);
                }
            }
            else
            {
                await _commentsDB.InsertAsync(combinedSpace);
            }
        }

        public async Task Clear()
        {
            _commentsDB.Dispose();
            _commentsDB = null;
            await DB.PurgeAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "//comments.ism");

            _commentsDB = await DB.CreateAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "//comments.ism", DBCreateFlags.None, ushort.MaxValue - 100, new DBKey[]
            {
                new DBKey(42, 0, DBKeyFlags.Alpha, "main", false, false, false, 0),
                new DBKey(8, 42, DBKeyFlags.AutoTime, "creation_timestamp", false, false, false, 1)
            });
        }

        public async Task StoreComments(Listing listing)
        {
            foreach (var comment in listing.Data.Children)
            {
                if (comment.Data is Comment)
                {
                    await StoreComment(comment);
                }
            }
        }

        private async Task<Listing> DeserializeCursor(DBCursor cursor, int count)
        {
            var targetListing = new Listing { Data = new ListingData { Children = new List<Thing>() } };
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
                    else if (i > count)
                    {
                        var comment = deserializedComment.Data as Comment;
                        targetListing.Data.After = "#c" + Encoding.UTF8.GetString(currentRecord, 0, 36);
                        break;
                    }
                } while (await cursor.MoveNextAsync());
            }

            return targetListing;
        }

        private async Task<Listing> GetChildren(string subredditId, string linkId, string parentId)
        {
            var keyspace = new byte[32];

            for (int i = 0; i < 8 && i < subredditId.Length; i++)
                keyspace[i] = (byte)subredditId[i];

            for (int i = 0; i < 12 && i < linkId.Length; i++)
                keyspace[i + 8] = (byte)linkId[i];

            for (int i = 0; i < 12 && i < parentId.Length; i++)
                keyspace[i + 20] = (byte)parentId[i];

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
                typedChild.Replies = await GetChildren(typedChild.SubredditId, typedChild.LinkId, typedChild.Name);
            }
        }

        public async Task<Listing> GetTopLevelComments(string subredditId, string linkId, int count)
        {
            //items that dont have a set parent id are top level
            var keyspace = new byte[32];

            for (int i = 0; i < 8 && i < subredditId.Length; i++)
                keyspace[i] = (byte)subredditId[i];

            for (int i = 0; i < 12 && i < linkId.Length; i++)
                keyspace[i + 8] = (byte)linkId[i];

            for (int i = 0; i < 12 && i < linkId.Length; i++)
                keyspace[i + 20] = (byte)linkId[i];

            var commentCursor = await _commentsDB.SelectAsync(_commentsDB.GetKeys().First(), keyspace);
            Listing topLevelChildren = null;
            using (commentCursor)
            {
                topLevelChildren = await DeserializeCursor(commentCursor, count);
            }
            await FillInChildren(topLevelChildren);
            return topLevelChildren;
            
        }

        public async Task<Listing> GetMoreComments(string after, int count)
        {
            var keyspace = new byte[32];
            var afterKeyspace = new byte[42];
            for (int i = 0; i < 32; i++)
            {
                keyspace[i] = (byte)after[i + 2];
            }

            for (int i = 0; i < 42; i++)
            {
                afterKeyspace[i] = (byte)after[i + 2];
            }

            var targetListing = new Listing { Data = new ListingData { Children = new List<Thing>() } };

            //descriminate to top level comments only
            var commentCursor = await _commentsDB.SelectAsync(_commentsDB.GetKeys().First(), keyspace);
            //move to the after target
            await commentCursor.SeekAsync(_commentsDB.GetKeys().First(), afterKeyspace);
            Listing topLevelChildren = null;
            using (commentCursor)
            {
                topLevelChildren = await DeserializeCursor(commentCursor, count);
            }
            await FillInChildren(topLevelChildren);
            return targetListing;
        }
    }
}
