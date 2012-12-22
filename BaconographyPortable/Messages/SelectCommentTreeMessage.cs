using BaconographyPortable.Model.Reddit;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    public class SelectCommentTreeMessage : MessageBase
    {
        public TypedThing<Link> LinkThing { get; set; }
        public TypedThing<Comment> RootComment { get; set; }
        public int Context { get; set; }
    }
}
