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
    class SelectCommentTree : MessageBase
    {
        public TypedThing<Link> LinkThing { get; set; }
        public TypedThing<Comment> RootComment { get; set; }
        public int Context { get; set; }
    }
}
