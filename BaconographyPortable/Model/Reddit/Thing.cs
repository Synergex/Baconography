using BaconographyPortable.Model.Reddit.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    public interface IThingData
    {
    }

    public class ThingData : IThingData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    [JsonConverter(typeof(ThingDataConverter))]
    public class Thing
    {

        [JsonProperty("kind")]
        public string Kind { get; set; }
        [JsonProperty("data")]
        public IThingData Data { get; set; }
    }

	[JsonConverter(typeof(TypedThingDataConverter))]
    public class TypedThing<T> : Thing where T : class ,IThingData
    {
        public TypedThing(Thing thing)
        {
            if (thing == null || !(thing.Data is T))
                throw new ArgumentException("thing null or incorrect data type");

            Kind = thing.Kind;
            base.Data = thing.Data;
        }
		[JsonProperty("data")]
        public new T Data
        {
            get
            {
                return base.Data as T;
            }
            set
            {
                base.Data = value;
            }
        }

		//[JsonConstructor()]
		public TypedThing(string kind, T data)
		{
			base.Kind = kind;
			base.Data = data;
		}
    }
}
