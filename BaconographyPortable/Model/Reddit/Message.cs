using BaconographyPortable.Model.Reddit.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    [DataContract]
    public class Message : ThingData
    {
        [JsonProperty("author")]
        public string Author { get; set; }
        [JsonProperty("body")]
        public string Body { get; set; }
        [JsonProperty("body_html")]
        public string BodyHtml { get; set; }
        [JsonProperty("context")]
        public string Context { get; set; }
        [JsonConverter(typeof(UnixTimeConverter))]
        [JsonProperty("created")]
        public DateTime Created { get; set; }
        [JsonConverter(typeof(UnixUTCTimeConverter))]
        [JsonProperty("created_utc")]
        public DateTime CreatedUTC { get; set; }
        [JsonProperty("dest")]
        public string Destination { get; set; }
        [JsonProperty("first_message")]
        public object FirstMessage { get; set; }
        [JsonProperty("first_message_name")]
        public string FirstMessageName { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("new")]
        public bool New { get; set; }
        [JsonProperty("parent_id")]
        public string ParentId { get; set; }
        [JsonProperty("replies")]
        public string Replies { get; set; }
        [JsonProperty("subject")]
        public string Subject { get; set; }
        [JsonProperty("subreddit")]
        public string Subreddit { get; set; }
        [JsonProperty("was_comment")]
        public bool WasComment { get; set; }

    }
}
