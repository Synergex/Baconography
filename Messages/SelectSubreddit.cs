using GalaSoft.MvvmLight.Messaging;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.Messages
{
    class SelectSubreddit : MessageBase
    {
        public TypedThing<Subreddit> Subreddit { get; set; }
    }
}
