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
    public class More : IThingData
    {
        [JsonProperty("children")]
        public List<string> Children { get; set; }
    }
}
