using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using KitaroDB;
using Newtonsoft.Json;
using Baconography.NeutralServices.KitaroDB.Util;
using BaconographyPortable.Model.KitaroDB.ListingHelpers;

namespace Baconography.NeutralServices.KitaroDB
{
    class UsageStatistics
    {
		private static string subredditStatisticsPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\subreddit_statistics_v2.ism";
        private static string domainStatisticsPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\domain_statistics_v2.ism";

        private static Task<UsageStatistics> _instanceTask;
        private static async Task<UsageStatistics> GetInstanceImpl()
        {
            var sub = await DB.CreateAsync(subredditStatisticsPath, DBCreateFlags.None, 28, new DBKey[]
            {
                new DBKey(12, 0, DBKeyFlags.Alpha, "subreddit_id", false, false, false, 0),
                new DBKey(4, 12, DBKeyFlags.Unsigned, "links", true, true, false, 1),
                new DBKey(4, 16, DBKeyFlags.Unsigned, "comments", true, true, false, 2),
                new DBKey(8, 20, DBKeyFlags.AutoTime, "update_timestamp", true, true, false, 3)
            });
            var domain = await DB.CreateAsync(domainStatisticsPath, DBCreateFlags.None, 20, new DBKey[]
            {
                new DBKey(4, 0, DBKeyFlags.Unsigned, "domain_hash", false, false, false, 0),
                new DBKey(4, 4, DBKeyFlags.Unsigned, "links", true, true, false, 1),
                new DBKey(4, 8, DBKeyFlags.Unsigned, "comments", true, true, false, 2),
                new DBKey(8, 12, DBKeyFlags.AutoTime, "update_timestamp", true, true, false, 3)
            });
            return new UsageStatistics(sub, domain);
        }

        public static Task<UsageStatistics> GetInstance()
        {
            if (_instanceTask == null)
            {
                _instanceTask = GetInstanceImpl();
            }
            return _instanceTask;
        }

        private UsageStatistics(DB subredditStats, DB domainStats)
        {
            _subredditStatisticsDB = subredditStats;
            _domainStatisticsDB = domainStats;
            _crc32 = new Crc32();
        }

        Crc32 _crc32;
        DB _subredditStatisticsDB;
        DB _domainStatisticsDB;

        const int SubIdKeySpaceSize = 12;
        const int DomainHashKeySpaceSize = 4;

        const int SubredditKeySpaceSize = 28;
        const int DomainKeySpaceSize = 20;

        private byte[] GenerateSubIdKeyspace(string id)
        {
            var keyspace = new byte[SubIdKeySpaceSize];

            for (int i = 0; i < SubIdKeySpaceSize && i < id.Length; i++)
                keyspace[i] = (byte)id[i];

            return keyspace;
        }

        private byte[] GenerateCombinedSubredditKeyspace(string id, uint links, uint comments)
        {
            var keyspace = new byte[SubredditKeySpaceSize];

            for (int i = 0; i < SubIdKeySpaceSize && i < id.Length; i++)
                keyspace[i] = (byte)id[i];

            BitConverter.GetBytes(links).CopyTo(keyspace, SubIdKeySpaceSize);
            BitConverter.GetBytes(comments).CopyTo(keyspace, SubIdKeySpaceSize + 4);

            return keyspace;
        }

        private byte[] GenerateDomainHashKeyspace(uint hash)
        {
            return BitConverter.GetBytes(hash);
        }

        private byte[] GenerateCombinedDomainKeyspace(uint hash, uint links, uint comments)
        {
            var keyspace = new byte[DomainKeySpaceSize];
            BitConverter.GetBytes(hash).CopyTo(keyspace, 0);
            BitConverter.GetBytes(links).CopyTo(keyspace, DomainHashKeySpaceSize);
            BitConverter.GetBytes(comments).CopyTo(keyspace, DomainHashKeySpaceSize + 4);

            return keyspace;
        }

        public async Task IncrementDomain(string domain, bool link)
        {
            uint links = 0;
            uint comments = 0;
            uint hash = Crc32.Compute(Crc32.StringGetBytes(domain));
            var keyspace = GenerateDomainHashKeyspace(hash);

            var dbCursor = await _domainStatisticsDB.SeekAsync(_domainStatisticsDB.GetKeys()[0], keyspace, DBReadFlags.AutoLock | DBReadFlags.WaitOnLock);
            if (dbCursor != null)
            {
                using (dbCursor)
                {
                    // Decode cursor
                    var record = dbCursor.Get();
                    links = BitConverter.ToUInt32(record.Skip(DomainHashKeySpaceSize).Take(4).ToArray(), 0);
                    comments = BitConverter.ToUInt32(record.Skip(DomainHashKeySpaceSize + 4).Take(4).ToArray(), 0);
                    // Increment variable
                    if (link)
                        links++;
                    else
                        comments++;
                    // Update record
                    var combinedSpace = GenerateCombinedDomainKeyspace(hash, links, comments);
                    await dbCursor.UpdateAsync(combinedSpace);
                }
            }
            else
            {
                links = (uint)(link ? 1 : 0);
                comments = (uint)(link ? 0 : 1);

                try
                {
                    if (link)
                        links++;
                    else
                        comments++;
                    // Insert a fresh, zero'd record
                    var combinedSpace = GenerateCombinedDomainKeyspace(hash, links, comments);
                    await _domainStatisticsDB.InsertAsync(combinedSpace);
                }
                catch (Exception ex)
                {
                }
            }
        }

        public async Task IncrementSubreddit(string id, bool link)
        {
            uint links = 0;
            uint comments = 0;
            var keyspace = GenerateSubIdKeyspace(id);

            var dbCursor = await _subredditStatisticsDB.SeekAsync(_subredditStatisticsDB.GetKeys()[0], keyspace, DBReadFlags.AutoLock | DBReadFlags.WaitOnLock);
            if (dbCursor != null)
            {
                using (dbCursor)
                {
                    // Decode cursor
                    var record = dbCursor.Get();
                    links = BitConverter.ToUInt32(record.Skip(SubIdKeySpaceSize).Take(4).ToArray(), 0);
                    comments = BitConverter.ToUInt32(record.Skip(SubIdKeySpaceSize + 4).Take(4).ToArray(), 0);
                    // Increment variable
                    if (link)
                        links++;
                    else
                        comments++;
                    // Update record
                    var combinedSpace = GenerateCombinedSubredditKeyspace(id, links, comments);
                    await dbCursor.UpdateAsync(combinedSpace);
                }
            }
            else
            {
                links = (uint)(link ? 1 : 0);
                comments = (uint)(link ? 0 : 1);

                try
                {
                    if (link)
                        links++;
                    else
                        comments++;
                    // Insert a fresh, zero'd record
                    var combinedSpace = GenerateCombinedSubredditKeyspace(id, links, comments);
                    await _subredditStatisticsDB.InsertAsync(combinedSpace);
                }
                catch (Exception ex)
                {
                }
            }
        }

        public async Task<Tuple<uint, uint>> GetSubredditAggregates(string id)
        {
            var keyspace = GenerateSubIdKeyspace(id);
            DBKey targetKey = _subredditStatisticsDB.GetKeys()[0];

            var cursor = await _subredditStatisticsDB.SeekAsync(targetKey, keyspace, DBReadFlags.NoLock);
            if (cursor != null)
            {
                using (cursor)
                {
                    var currentRecord = cursor.Get();
                    uint links = BitConverter.ToUInt32(currentRecord.Skip(SubIdKeySpaceSize).Take(4).ToArray(), 0);
                    uint comments = BitConverter.ToUInt32(currentRecord.Skip(SubIdKeySpaceSize + 4).Take(4).ToArray(), 0);
                    return new Tuple<uint, uint>(links, comments);
                }
            }
            else
                return null;
        }

        public async Task<Tuple<uint, uint>> GetDomainAggregates(string domain)
        {
            uint hash = Crc32.Compute(Crc32.StringGetBytes(domain));
            var keyspace = GenerateDomainHashKeyspace(hash);
            DBKey targetKey = _domainStatisticsDB.GetKeys()[0];

            var cursor = await _domainStatisticsDB.SeekAsync(targetKey, keyspace, DBReadFlags.NoLock);
            if (cursor != null)
            {
                using (cursor)
                {
                    var currentRecord = cursor.Get();
                    var links = BitConverter.ToUInt32(currentRecord.Skip(DomainHashKeySpaceSize).Take(4).ToArray(), 0);
                    var comments = BitConverter.ToUInt32(currentRecord.Skip(DomainHashKeySpaceSize + 4).Take(4).ToArray(), 0);
                    return new Tuple<uint, uint>(links, comments);
                }
            }
            else
                return null;
        }

        public async Task<List<SubredditAggregate>> GetSubredditAggregateList(int maxSize, int threshold)
        {
            var retval = new List<SubredditAggregate>();
            var cursor = await _subredditStatisticsDB.SeekAsync(DBReadFlags.NoLock);

            while (cursor != null)
            {
                var agg = new SubredditAggregate();
                var currentRecord = cursor.Get();
                agg.SubredditId = BitConverter.ToString(currentRecord.Take(12).ToArray(), 0);
                agg.LinkClicks = BitConverter.ToUInt32(currentRecord.Skip(SubIdKeySpaceSize).Take(4).ToArray(), 0);
                agg.CommentClicks = BitConverter.ToUInt32(currentRecord.Skip(SubIdKeySpaceSize + 4).Take(4).ToArray(), 0);
                agg.LastModified = new DateTime(BitConverter.ToInt64(currentRecord.Skip(20).Take(8).ToArray(), 0));
                if (agg.LinkClicks > threshold || agg.CommentClicks > threshold)
                    retval.Add(agg);
                if (!await cursor.MoveNextAsync())
                {
                    cursor.Dispose();
                    cursor = null;
                }
            }

            retval = retval.Where(p => p.CommentClicks > threshold || p.LinkClicks > threshold)
                .OrderBy(p => p.CommentClicks > p.LinkClicks ? p.CommentClicks : p.LinkClicks)
                .Take(maxSize)
                .ToList();
            return retval;
        }

        public async Task<List<DomainAggregate>> GetDomainAggregateList(int maxSize, int threshold)
        {
            var retval = new List<DomainAggregate>();
            var cursor = await _domainStatisticsDB.SeekAsync(DBReadFlags.NoLock);

            while (cursor != null)
            {
                var agg = new DomainAggregate();
                var currentRecord = cursor.Get();
                agg.DomainHash = BitConverter.ToUInt32(currentRecord.Take(4).ToArray(), 0);
                agg.LinkClicks = BitConverter.ToUInt32(currentRecord.Skip(4).Take(4).ToArray(), 0);
                agg.CommentClicks = BitConverter.ToUInt32(currentRecord.Skip(8).Take(4).ToArray(), 0);
                agg.LastModified = new DateTime(BitConverter.ToInt64(currentRecord.Skip(12).Take(8).ToArray(), 0));
                if (agg.LinkClicks > threshold || agg.CommentClicks > threshold)
                    retval.Add(agg);
                if (!await cursor.MoveNextAsync())
                {
                    cursor.Dispose();
                    cursor = null;
                }
            }

            retval = retval.Where(p => p.CommentClicks > threshold || p.LinkClicks > threshold)
                .OrderBy(p => p.CommentClicks > p.LinkClicks ? p.CommentClicks : p.LinkClicks)
                .Take(maxSize)
                .ToList();
            return retval;
        }
    }
}
