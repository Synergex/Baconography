using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.KitaroDB.ListingHelpers
{
    public class UsageStatisticsAggregate
    {
        public uint LinkClicks { get; set; }
        public uint CommentClicks { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class DomainAggregate : UsageStatisticsAggregate
    {
        public string Domain { get; set; }
        public uint DomainHash { get; set; }
    }

    public class SubredditAggregate : UsageStatisticsAggregate
    {
        public string SubredditId { get; set; }
    }
}
