using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Things
{
    public class Listing
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }
        [JsonProperty("data")]
        public ListingData Data { get; set; }
    }

    public class ListingData
    {
        [JsonProperty("modhash")]
        public string ModHash { get; set; }

        [JsonProperty("children")]
        public List<Thing> Children { get; set; }

        [JsonProperty("after")]
        public string After { get; set; }
        [JsonProperty("before")]
        public string Before { get; set; }
    }
}
