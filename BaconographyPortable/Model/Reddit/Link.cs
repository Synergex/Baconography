using BaconographyPortable.Model.Reddit.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    public class Link : ThingData, ICreated, IVotable
    {
        [JsonConverter(typeof(UnixTimeConverter))]
        [JsonProperty("created")]
        public DateTime Created { get; set; }
        [JsonConverter(typeof(UnixUTCTimeConverter))]
        [JsonProperty("created_utc")]

        public DateTime CreatedUTC { get; set; }
        [JsonProperty("author")]
        public string Author { get; set; }
        [JsonProperty("author_flair_css_class")]
        public string AuthorFlairCssClass { get; set; }
        [JsonProperty("author_flair_text")]
        public string AuthorFlairText { get; set; }
        [JsonProperty("clicked")]
        public bool Clicked { get; set; }
        [JsonProperty("domain")]
        public string Domain { get; set; }
        [JsonProperty("hidden")]
        public bool Hidden { get; set; }
        [JsonProperty("is_self")]
        public bool IsSelf { get; set; }
        [JsonProperty("media")]
        public object Media { get; set; }
        [JsonProperty("media_embed")]
        public MediaEmbed MediaEmbed { get; set; }
        [JsonProperty("num_comments")]
        public int CommentCount { get; set; }
        [JsonProperty("over18")]
        public bool Over18 { get; set; }
        [JsonProperty("permalink")]
        public string Permalink { get; set; }
        [JsonProperty("saved")]
        public bool Saved { get; set; }
        [JsonProperty("score")]
        public int Score { get; set; }
        [JsonProperty("selftext")]
        public string Selftext { get; set; }
        [JsonProperty("selftext_html")]
        public string SelftextHtml { get; set; }
        [JsonProperty("subreddit")]
        public string Subreddit { get; set; }
        [JsonProperty("subreddit_id")]
        public string SubredditId { get; set; }
        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("ups")]
        public int Ups { get; set; }
        [JsonProperty("downs")]
        public int Downs { get; set; }
        [JsonProperty("likes")]
        public bool? Likes { get; set; }
    }

    public class MediaEmbed
    {
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
    }
}
