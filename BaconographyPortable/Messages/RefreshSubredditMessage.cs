using BaconographyPortable.Model.Reddit;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
	public class RefreshSubredditMessage : MessageBase
    {
        public TypedThing<Subreddit> Subreddit { get; set; }
    }
}
