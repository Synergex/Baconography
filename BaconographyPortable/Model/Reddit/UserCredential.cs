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
    public class UserCredential
    {
		[JsonProperty("logincookie")]
        public string LoginCookie { get; set; }
		[JsonProperty("username")]
        public string Username { get; set; }
		[JsonProperty("me")]
        public Thing Me { get; set; }
		[JsonProperty("isdefault")]
        public bool IsDefault { get; set; }
    }
}
