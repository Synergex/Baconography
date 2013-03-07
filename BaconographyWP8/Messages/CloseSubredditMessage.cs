using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.Messages
{
    public class CloseSubredditMessage : MessageBase
    {
        public TypedThing<Subreddit> Subreddit { get; set; }
		public string Heading { get; set; }
    }
}
