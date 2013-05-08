using BaconographyPortable.Model.Reddit;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    public class SelectSubredditMessage : MessageBase
    {
        public TypedThing<Subreddit> Subreddit { get; set; }
		public bool AddOnly { get; set; }
    }
}
