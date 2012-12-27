using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.Converters
{
    public class ThingDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Thing);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Thing targetThing = new Thing();

            if (reader.TokenType == JsonToken.Null)
                return null;

            while (reader.TokenType != JsonToken.EndObject)
            {
                reader.Read(); // startobject

                switch ((string)reader.Value)
                {
                    case "kind":
                        {
                            reader.Read(); //get the kind value
                            targetThing.Kind = (string)reader.Value;

                            switch (targetThing.Kind)
                            {
                                case "t1":
                                    targetThing.Data = new Comment();
                                    break;
                                case "t2":
                                    targetThing.Data = new Account();
                                    break;
                                case "t3":
                                    targetThing.Data = new Link();
                                    break;
                                case "t4":
                                    targetThing.Data = new Message();
                                    break;
                                case "t5":
                                    targetThing.Data = new Subreddit();
                                    break;
                                case "more":
                                    targetThing.Data = new More();
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            break;
                        }
                    case "data":
                        {
                            reader.Read(); //move to inner object
                            serializer.Populate(reader, targetThing.Data);
                            break;
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
            reader.Read(); //close the current object
            return targetThing;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("kind");
            writer.WriteValue(((Thing)value).Kind);
            writer.WritePropertyName("data");
            serializer.Serialize(writer, ((Thing)value).Data);

            writer.WriteEndObject();
        }
    }
}
