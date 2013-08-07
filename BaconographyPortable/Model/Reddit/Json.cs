using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
	public class JsonThing
    {
        [JsonProperty("json")]
        public JsonData Json { get; set; }
    }

    public class LoginJsonThing
    {
        [JsonProperty("json")]
        public LoginJsonData Json { get; set; }
    }

    public class LoginJsonData : IThingData
    {
        [JsonProperty("data")]
        public JsonData3 Data { get; set; }
        [JsonProperty("errors")]
        public Object[] Errors { get; set; }
    }

	public class JsonData3 : IThingData
    {
        [JsonProperty("modhash")]
        public string Modhash { get; set; }
        [JsonProperty("cookie")]
        public string Cookie { get; set; }
    }

	public class JsonData : IThingData
    {
        [JsonProperty("data")]
        public JsonData2 Data { get; set; }
        [JsonProperty("errors")]
        public Object[] Errors { get; set; }
    }

	public class JsonData2 : IThingData
    {
        [JsonProperty("things")]
        public List<Thing> Things { get; set; }
    }

    [DataContract]
    public class CaptchaJsonData : IThingData
    {
        [JsonProperty("captcha")]
        public string Captcha { get; set; }
        [JsonProperty("errors")]
        public string[] Errors { get; set; }
    }
}
