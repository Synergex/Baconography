using BaconographyPortable.Model.Reddit;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    public class SelectCommentTreeMessage : MessageBase
    {
		[JsonProperty("LinkThing")]
        public TypedThing<Link> LinkThing { get; set; }
		[JsonProperty("RootComment")]
        public TypedThing<Comment> RootComment { get; set; }
		[JsonProperty("Context")]
        public int Context { get; set; }

		public SelectCommentTreeMessage()
		{

		}
    }
}
