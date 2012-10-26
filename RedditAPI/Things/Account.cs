using Newtonsoft.Json;
using Baconography.RedditAPI.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Things
{
    public class Account : ThingData
    {
        [JsonProperty("comment_karma")]
        public int CommentKarma { get; set; }
        [JsonConverter(typeof(UnixTimeConverter))]
        [JsonProperty("created")]
        public DateTime Created { get; set; }
        [JsonConverter(typeof(UnixUTCTimeConverter))]
        [JsonProperty("created_utc")]
        public DateTime CreatedUTC { get; set; }

        [JsonProperty("has_mail", NullValueHandling=NullValueHandling.Ignore, DefaultValueHandling=DefaultValueHandling.Populate)]
        public bool HasMail { get; set; }
        [JsonProperty("has_mod_mail", NullValueHandling=NullValueHandling.Ignore, DefaultValueHandling=DefaultValueHandling.Populate)]
        public bool HasModMail { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("is_gold", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IsGold { get; set; }
        [JsonProperty("is_mod", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IsMod { get; set; }
        [JsonProperty("link_karma", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Populate)]
        public int LinkKarma { get; set; }
        [JsonProperty("modhash")]
        public string ModHash { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
