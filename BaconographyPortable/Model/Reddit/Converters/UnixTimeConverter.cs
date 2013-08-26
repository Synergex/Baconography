using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.Converters
{
    public class UnixUTCTimeConverter : JsonConverter
    {
        private static DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return _epoch.AddSeconds((long)((double)reader.Value));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((DateTime)value - _epoch).TotalSeconds);
        }
    }

    public class UnixTimeConverter : JsonConverter
    {
        private static DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var nullableDouble = reader.Value as Nullable<double>;
            if (nullableDouble != null)
                return _epoch.AddSeconds((long)(nullableDouble ?? 0));
            else
            {
                var nullableLong = reader.Value as Nullable<long>;
                if (nullableLong != null)
                    return _epoch.AddSeconds(nullableLong ?? 0);
            }
            return _epoch;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((DateTime)value - _epoch).TotalSeconds);
        }
    }
}
