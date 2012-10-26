using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Things
{
    class JsonThing
    {
        [JsonProperty("json")]
        public JsonData Json { get; set; }
    }

    class LoginJsonThing
    {
        [JsonProperty("json")]
        public LoginJsonData Json { get; set; }
    }

    class LoginJsonData : IThingData
    {
        [JsonProperty("data")]
        public JsonData3 Data { get; set; }
        [JsonProperty("errors")]
        public Object[] Errors { get; set; }
    }

    class JsonData3 : IThingData
    {
        [JsonProperty("modhash")]
        public string Modhash { get; set; }
        [JsonProperty("cookie")]
        public string Cookie { get; set; }
    }

    class JsonData : IThingData
    {
        [JsonProperty("data")]
        public JsonData2 Data { get; set; }
        [JsonProperty("errors")]
        public Object[] Errors { get; set; }
    }

    class JsonData2 : IThingData
    {
        [JsonProperty("things")]
        public List<Thing> Things { get; set; }
    }
}
