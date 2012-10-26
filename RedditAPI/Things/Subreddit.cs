using Newtonsoft.Json;
using Baconography.RedditAPI.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Things
{
    public class Subreddit : ThingData, ICreated
    {
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }
        [JsonProperty("over18")]
        public bool Over18 { get; set; }
        [JsonProperty("subscribers")]
        public long Subscribers { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonConverter(typeof(UnixTimeConverter))]
        [JsonProperty("created")]
        public DateTime Created { get; set; }
        [JsonConverter(typeof(UnixUTCTimeConverter))]
        [JsonProperty("created_utc")]
        public DateTime CreatedUTC { get; set; }
        [JsonProperty("header_img")]
        public string HeaderImage { get; set; }
        [JsonProperty("header_size")]
        public int[] HeaderSize { get; set; }
        [JsonProperty("public_description")]
        public string PublicDescription { get; set; }
        [JsonProperty("header_title")]
        public string Headertitle { get; set; }
    }
}
