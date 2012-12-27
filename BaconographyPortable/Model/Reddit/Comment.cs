using BaconographyPortable.Model.Reddit.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    public class Comment : ThingData, IVotable, ICreated
    {
        [JsonProperty("author")]
        public string Author { get; set; }
        [JsonProperty("author_flair_css_class")]
        public string AuthorFlairCssClass { get; set; }
        [JsonProperty("author_flair_text")]
        public string AuthorFlairText { get; set; }
        [JsonProperty("body")]
        public string Body { get; set; }
        [JsonProperty("body_html")]
        public string BodyHtml { get; set; }
        [JsonProperty("link_id")]
        public string LinkId { get; set; }
        [JsonProperty("parent_id")]
        public string ParentId { get; set; }
        [JsonProperty("subreddit")]
        public string Subreddit { get; set; }
        [JsonProperty("subreddit_id")]
        public string SubredditId { get; set; }
        [JsonProperty("replies")]
        public Listing Replies { get; set; }

        [JsonConverter(typeof(UnixTimeConverter))]
        [JsonProperty("created")]
        public DateTime Created { get; set; }
        [JsonConverter(typeof(UnixUTCTimeConverter))]
        [JsonProperty("created_utc")]
        public DateTime CreatedUTC { get; set; }

        [JsonProperty("ups")]
        public int Ups { get; set; }
        [JsonProperty("downs")]
        public int Downs { get; set; }
        [JsonProperty("likes")]
        public bool? Likes { get; set; }
    }
}
