//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.Serialization;
//using System.Text;
//using System.Threading.Tasks;

//namespace BaconographyWP8BackgroundTask.Hacks
//{
//    public interface IThingData
//    {
//    }

//    [DataContract]
//    public class ThingData : IThingData
//    {
//        [JsonProperty("id")]
//        public string Id { get; set; }
//        [JsonProperty("name")]
//        public string Name { get; set; }
//    }
//    [DataContract]
//    [JsonConverter(typeof(ThingDataConverter))]
//    public class Thing
//    {

//        [JsonProperty("kind")]
//        public string Kind { get; set; }
//        [JsonProperty("data")]
//        public IThingData Data { get; set; }
//    }

//    [DataContract]
//    [JsonConverter(typeof(TypedThingDataConverter))]
//    public class TypedThing<T> : Thing where T : class ,IThingData
//    {
//        public TypedThing(Thing thing)
//        {
//            if (thing == null || !(thing.Data is T))
//                throw new ArgumentException("thing null or incorrect data type");

//            Kind = thing.Kind;
//            base.Data = thing.Data;
//        }
//        [JsonProperty("data")]
//        public new T Data
//        {
//            get
//            {
//                return base.Data as T;
//            }
//            set
//            {
//                base.Data = value;
//            }
//        }

//        // XAML friendly data wrapper
//        public T TypedData
//        {
//            get
//            {
//                return Data;
//            }
//        }

//        //[JsonConstructor()]
//        public TypedThing(string kind, T data)
//        {
//            base.Kind = kind;
//            base.Data = data;
//        }
//    }

//    public class ThingDataConverter : JsonConverter
//    {
//        public override bool CanConvert(Type objectType)
//        {
//            return objectType == typeof(Thing);
//        }

//        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//        {
//            Thing targetThing = new Thing();

//            if (reader.TokenType == JsonToken.Null)
//                return null;

//            while (reader.TokenType != JsonToken.EndObject)
//            {
//                reader.Read(); // startobject

//                switch ((string)reader.Value)
//                {
//                    case "kind":
//                        {
//                            reader.Read(); //get the kind value
//                            targetThing.Kind = (string)reader.Value;

//                            switch (targetThing.Kind)
//                            {
//                                case "t2":
//                                    targetThing.Data = new Account();
//                                    break;
//                                case "t3":
//                                    targetThing.Data = new Link();
//                                    break;
//                                case "t4":
//                                    targetThing.Data = new Message();
//                                    break;
//                                case "t4.5":
//                                    targetThing.Data = new CommentMessage();
//                                    break;
//                                default:
//                                    throw new NotImplementedException();
//                            }
//                            break;
//                        }
//                    case "data":
//                        {
//                            reader.Read(); //move to inner object
//                            serializer.Populate(reader, targetThing.Data);
//                            break;
//                        }
//                    default:
//                        throw new NotImplementedException();
//                }
//            }
//            reader.Read(); //close the current object
//            return targetThing;
//        }

//        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//        {
//            writer.WriteStartObject();
//            writer.WritePropertyName("kind");
//            writer.WriteValue(((Thing)value).Kind);
//            writer.WritePropertyName("data");
//            serializer.Serialize(writer, ((Thing)value).Data);

//            writer.WriteEndObject();
//        }
//    }

//    public class TypedThingDataConverter : JsonConverter
//    {
//        public override bool CanConvert(Type objectType)
//        {
//            return objectType == typeof(Thing);
//        }

//        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//        {
//            Thing targetThing = new Thing();
//            Type dataType = typeof(Thing);

//            if (reader.TokenType == JsonToken.Null)
//                return null;

//            while (reader.TokenType != JsonToken.EndObject)
//            {
//                reader.Read(); // startobject

//                switch ((string)reader.Value)
//                {
//                    case "kind":
//                        {
//                            reader.Read(); //get the kind value
//                            targetThing.Kind = (string)reader.Value;

//                            switch (targetThing.Kind)
//                            {
//                                case "t2":
//                                    targetThing.Data = new Account();
//                                    dataType = typeof(Account);
//                                    break;
//                                case "t3":
//                                    targetThing.Data = new Link();
//                                    dataType = typeof(Link);
//                                    break;
//                                case "t4":
//                                    targetThing.Data = new Message();
//                                    dataType = typeof(Message);
//                                    break;
//                                case "t4.5":
//                                    targetThing.Data = new CommentMessage();
//                                    dataType = typeof(CommentMessage);
//                                    break;
//                                default:
//                                    throw new NotImplementedException();
//                            }
//                            break;
//                        }
//                    case "data":
//                        {
//                            reader.Read(); //move to inner object
//                            serializer.Populate(reader, targetThing.Data);
//                            break;
//                        }
//                    default:
//                        throw new NotImplementedException();
//                }
//            }
//            reader.Read(); //close the current object

//            Type genericType = typeof(TypedThing<>);
//            Type[] typeArgs = { dataType };
//            Type typedThing = genericType.MakeGenericType(typeArgs);

//            return Activator.CreateInstance(typedThing, new object[] { targetThing });
//        }

//        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//        {
//            writer.WriteStartObject();
//            writer.WritePropertyName("kind");
//            writer.WriteValue(((Thing)value).Kind);
//            writer.WritePropertyName("data");
//            serializer.Serialize(writer, ((Thing)value).Data);

//            writer.WriteEndObject();
//        }
//    }

//    [DataContract]
//    public class CommentMessage : Message
//    {
//        [JsonProperty("link_title")]
//        public string LinkTitle { get; set; }
//        [JsonProperty("likes")]
//        public bool? Likes { get; set; }
//    }

//    [DataContract]
//    public class Link : ThingData
//    {
//        [JsonProperty("created")]
//        public string Created { get; set; }
//        [JsonProperty("created_utc")]

//        public string CreatedUTC { get; set; }
//        [JsonProperty("author")]
//        public string Author { get; set; }
//        [JsonProperty("author_flair_css_class")]
//        public string AuthorFlairCssClass { get; set; }
//        [JsonProperty("author_flair_text")]
//        public string AuthorFlairText { get; set; }
//        [JsonProperty("clicked")]
//        public bool Clicked { get; set; }
//        [JsonProperty("domain")]
//        public string Domain { get; set; }
//        [JsonProperty("hidden")]
//        public bool Hidden { get; set; }
//        [JsonProperty("is_self")]
//        public bool IsSelf { get; set; }
//        [JsonProperty("media")]
//        public object Media { get; set; }
//        [JsonProperty("media_embed")]
//        public MediaEmbed MediaEmbed { get; set; }
//        [JsonProperty("num_comments")]
//        public int CommentCount { get; set; }
//        [JsonProperty("over_18")]
//        public bool Over18 { get; set; }
//        [JsonProperty("permalink")]
//        public string Permalink { get; set; }
//        [JsonProperty("saved")]
//        public bool Saved { get; set; }
//        [JsonProperty("score")]
//        public int Score { get; set; }
//        [JsonProperty("selftext")]
//        public string Selftext { get; set; }
//        [JsonProperty("selftext_html")]
//        public string SelftextHtml { get; set; }
//        [JsonProperty("subreddit")]
//        public string Subreddit { get; set; }
//        [JsonProperty("subreddit_id")]
//        public string SubredditId { get; set; }
//        [JsonProperty("thumbnail")]
//        public string Thumbnail { get; set; }
//        [JsonProperty("title")]
//        public string Title { get; set; }
//        [JsonProperty("url")]
//        public string Url { get; set; }

//        [JsonProperty("ups")]
//        public int Ups { get; set; }
//        [JsonProperty("downs")]
//        public int Downs { get; set; }
//        [JsonProperty("likes")]
//        public bool? Likes { get; set; }
//    }

//    public class MediaEmbed
//    {
//        [JsonProperty("content")]
//        public string Content { get; set; }
//        [JsonProperty("width")]
//        public int Width { get; set; }
//        [JsonProperty("height")]
//        public int Height { get; set; }
//    }

//    [DataContract]
//    public class Message : ThingData
//    {
//        [JsonProperty("author")]
//        public string Author { get; set; }
//        [JsonProperty("body")]
//        public string Body { get; set; }
//        [JsonProperty("body_html")]
//        public string BodyHtml { get; set; }
//        [JsonProperty("context")]
//        public string Context { get; set; }
//        [JsonProperty("created")]
//        public string Created { get; set; }
//        [JsonProperty("created_utc")]
//        public string CreatedUTC { get; set; }
//        [JsonProperty("dest")]
//        public string Destination { get; set; }
//        [JsonProperty("first_message")]
//        public object FirstMessage { get; set; }
//        [JsonProperty("first_message_name")]
//        public string FirstMessageName { get; set; }
//        [JsonProperty("id")]
//        public string Id { get; set; }
//        [JsonProperty("name")]
//        public string Name { get; set; }
//        [JsonProperty("new")]
//        public bool New { get; set; }
//        [JsonProperty("parent_id")]
//        public string ParentId { get; set; }
//        [JsonProperty("replies")]
//        public string Replies { get; set; }
//        [JsonProperty("subject")]
//        public string Subject { get; set; }
//        [JsonProperty("subreddit")]
//        public string Subreddit { get; set; }
//        [JsonProperty("was_comment")]
//        public bool WasComment { get; set; }
//    }
//    [DataContract]
//    public class User
//    {
//        public string Username { get; set; }
//        public string LoginCookie { get; set; }
//        public bool Authenticated { get; set; }
//        public Account Me { get; set; }
//        public bool NeedsCaptcha { get; set; }
//    }

//    public class Account : ThingData
//    {
//        [JsonProperty("comment_karma")]
//        public int CommentKarma { get; set; }
//        [JsonProperty("created")]
//        public string Created { get; set; }
//        [JsonProperty("created_utc")]
//        public string CreatedUTC { get; set; }

//        [JsonProperty("has_mail", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Populate)]
//        public bool HasMail { get; set; }
//        [JsonProperty("has_mod_mail", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Populate)]
//        public bool HasModMail { get; set; }
//        [JsonProperty("id")]
//        public string Id { get; set; }
//        [JsonProperty("is_gold", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Populate)]
//        public bool IsGold { get; set; }
//        [JsonProperty("is_mod", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Populate)]
//        public bool IsMod { get; set; }
//        [JsonProperty("link_karma", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Populate)]
//        public int LinkKarma { get; set; }
//        [JsonProperty("modhash")]
//        public string ModHash { get; set; }
//        [JsonProperty("name")]
//        public string Name { get; set; }
//    }

//    [DataContract]
//    public class UserCredential
//    {
//        [JsonProperty("logincookie")]
//        public string LoginCookie { get; set; }
//        [JsonProperty("username")]
//        public string Username { get; set; }
//        [JsonProperty("me")]
//        public Thing Me { get; set; }
//        [JsonProperty("isdefault")]
//        public bool IsDefault { get; set; }
//    }

//    [DataContract]
//    public class Listing
//    {
//        [JsonProperty("kind")]
//        public string Kind { get; set; }
//        [JsonProperty("data")]
//        public ListingData Data { get; set; }
//    }

//    [DataContract]
//    public class ListingData
//    {
//        [JsonProperty("modhash")]
//        public string ModHash { get; set; }

//        [JsonProperty("children")]
//        public List<Thing> Children { get; set; }

//        [JsonProperty("after")]
//        public string After { get; set; }
//        [JsonProperty("before")]
//        public string Before { get; set; }
//    }

//    public class JsonThing
//    {
//        [JsonProperty("json")]
//        public JsonData Json { get; set; }
//    }

//    public class LoginJsonThing
//    {
//        [JsonProperty("json")]
//        public LoginJsonData Json { get; set; }
//    }

//    public class LoginJsonData : IThingData
//    {
//        [JsonProperty("data")]
//        public JsonData3 Data { get; set; }
//        [JsonProperty("errors")]
//        public Object[] Errors { get; set; }
//    }

//    public class JsonData3 : IThingData
//    {
//        [JsonProperty("modhash")]
//        public string Modhash { get; set; }
//        [JsonProperty("cookie")]
//        public string Cookie { get; set; }
//    }

//    public class JsonData : IThingData
//    {
//        [JsonProperty("data")]
//        public JsonData2 Data { get; set; }
//        [JsonProperty("errors")]
//        public Object[] Errors { get; set; }
//    }

//    public class JsonData2 : IThingData
//    {
//        [JsonProperty("things")]
//        public List<Thing> Things { get; set; }
//    }

//    [DataContract]
//    public class CaptchaJsonData : IThingData
//    {
//        [JsonProperty("captcha")]
//        public string Captcha { get; set; }
//        [JsonProperty("errors")]
//        public string[] Errors { get; set; }
//    }
//}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Procurios.Public
{
    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// Spec. details, see http://www.json.org/
    ///
    /// JSON uses Arrays and Objects. These correspond here to the datatypes ArrayList and Hashtable.
    /// All numbers are parsed to doubles.
    /// </summary>
    public class JSON
    {
        public const int TOKEN_NONE = 0;
        public const int TOKEN_CURLY_OPEN = 1;
        public const int TOKEN_CURLY_CLOSE = 2;
        public const int TOKEN_SQUARED_OPEN = 3;
        public const int TOKEN_SQUARED_CLOSE = 4;
        public const int TOKEN_COLON = 5;
        public const int TOKEN_COMMA = 6;
        public const int TOKEN_STRING = 7;
        public const int TOKEN_NUMBER = 8;
        public const int TOKEN_TRUE = 9;
        public const int TOKEN_FALSE = 10;
        public const int TOKEN_NULL = 11;

        private const int BUILDER_CAPACITY = 2000;

        /// <summary>
        /// Parses the string json into a value
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
        public static object JsonDecode(string json)
        {
            bool success = true;

            return JsonDecode(json, ref success);
        }

        public static object GetValue(object obj, string name)
        {
            var dict = obj as Dictionary<string, object>;
            return dict[name];
        }

        /// <summary>
        /// Parses the string json into a value; and fills 'success' with the successfullness of the parse.
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <param name="success">Successful parse?</param>
        /// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
        public static object JsonDecode(string json, ref bool success)
        {
            success = true;
            if (json != null)
            {
                char[] charArray = json.ToCharArray();
                int index = 0;
                object value = ParseValue(charArray, ref index, ref success);
                return value;
            }
            else
            {
                return null;
            }
        }

        protected static Dictionary<string, object> ParseObject(char[] json, ref int index, ref bool success)
        {
            var table = new Dictionary<string, object>();
            int token;

            // {
            NextToken(json, ref index);

            bool done = false;
            while (!done)
            {
                token = LookAhead(json, index);
                if (token == JSON.TOKEN_NONE)
                {
                    success = false;
                    return null;
                }
                else if (token == JSON.TOKEN_COMMA)
                {
                    NextToken(json, ref index);
                }
                else if (token == JSON.TOKEN_CURLY_CLOSE)
                {
                    NextToken(json, ref index);
                    return table;
                }
                else
                {

                    // name
                    string name = ParseString(json, ref index, ref success);
                    if (!success)
                    {
                        success = false;
                        return null;
                    }

                    // :
                    token = NextToken(json, ref index);
                    if (token != JSON.TOKEN_COLON)
                    {
                        success = false;
                        return null;
                    }

                    // value
                    object value = ParseValue(json, ref index, ref success);
                    if (!success)
                    {
                        success = false;
                        return null;
                    }

                    table[name] = value;
                }
            }

            return table;
        }

        protected static List<Object> ParseArray(char[] json, ref int index, ref bool success)
        {
            var array = new List<Object>();

            // [
            NextToken(json, ref index);

            bool done = false;
            while (!done)
            {
                int token = LookAhead(json, index);
                if (token == JSON.TOKEN_NONE)
                {
                    success = false;
                    return null;
                }
                else if (token == JSON.TOKEN_COMMA)
                {
                    NextToken(json, ref index);
                }
                else if (token == JSON.TOKEN_SQUARED_CLOSE)
                {
                    NextToken(json, ref index);
                    break;
                }
                else
                {
                    object value = ParseValue(json, ref index, ref success);
                    if (!success)
                    {
                        return null;
                    }

                    array.Add(value);
                }
            }

            return array;
        }

        protected static object ParseValue(char[] json, ref int index, ref bool success)
        {
            switch (LookAhead(json, index))
            {
                case JSON.TOKEN_STRING:
                    return ParseString(json, ref index, ref success);
                case JSON.TOKEN_NUMBER:
                    return ParseNumber(json, ref index, ref success);
                case JSON.TOKEN_CURLY_OPEN:
                    return ParseObject(json, ref index, ref success);
                case JSON.TOKEN_SQUARED_OPEN:
                    return ParseArray(json, ref index, ref success);
                case JSON.TOKEN_TRUE:
                    NextToken(json, ref index);
                    return true;
                case JSON.TOKEN_FALSE:
                    NextToken(json, ref index);
                    return false;
                case JSON.TOKEN_NULL:
                    NextToken(json, ref index);
                    return null;
                case JSON.TOKEN_NONE:
                    break;
            }

            success = false;
            return null;
        }

        protected static string ParseString(char[] json, ref int index, ref bool success)
        {
            StringBuilder s = new StringBuilder(BUILDER_CAPACITY);
            char c;

            EatWhitespace(json, ref index);

            // "
            c = json[index++];

            bool complete = false;
            while (!complete)
            {

                if (index == json.Length)
                {
                    break;
                }

                c = json[index++];
                if (c == '"')
                {
                    complete = true;
                    break;
                }
                else if (c == '\\')
                {

                    if (index == json.Length)
                    {
                        break;
                    }
                    c = json[index++];
                    if (c == '"')
                    {
                        s.Append('"');
                    }
                    else if (c == '\\')
                    {
                        s.Append('\\');
                    }
                    else if (c == '/')
                    {
                        s.Append('/');
                    }
                    else if (c == 'b')
                    {
                        s.Append('\b');
                    }
                    else if (c == 'f')
                    {
                        s.Append('\f');
                    }
                    else if (c == 'n')
                    {
                        s.Append('\n');
                    }
                    else if (c == 'r')
                    {
                        s.Append('\r');
                    }
                    else if (c == 't')
                    {
                        s.Append('\t');
                    }
                    else if (c == 'u')
                    {
                        int remainingLength = json.Length - index;
                        if (remainingLength >= 4)
                        {
                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint;
                            if (!(success = UInt32.TryParse(new string(json, index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint)))
                            {
                                return "";
                            }
                            // convert the integer codepoint to a unicode char and add to string
                            s.Append(Char.ConvertFromUtf32((int)codePoint));
                            // skip 4 chars
                            index += 4;
                        }
                        else
                        {
                            break;
                        }
                    }

                }
                else
                {
                    s.Append(c);
                }

            }

            if (!complete)
            {
                success = false;
                return null;
            }

            return s.ToString();
        }

        protected static double ParseNumber(char[] json, ref int index, ref bool success)
        {
            EatWhitespace(json, ref index);

            int lastIndex = GetLastIndexOfNumber(json, index);
            int charLength = (lastIndex - index) + 1;

            double number;
            success = Double.TryParse(new string(json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out number);

            index = lastIndex + 1;
            return number;
        }

        protected static int GetLastIndexOfNumber(char[] json, int index)
        {
            int lastIndex;

            for (lastIndex = index; lastIndex < json.Length; lastIndex++)
            {
                if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
                {
                    break;
                }
            }
            return lastIndex - 1;
        }

        protected static void EatWhitespace(char[] json, ref int index)
        {
            for (; index < json.Length; index++)
            {
                if (" \t\n\r".IndexOf(json[index]) == -1)
                {
                    break;
                }
            }
        }

        protected static int LookAhead(char[] json, int index)
        {
            int saveIndex = index;
            return NextToken(json, ref saveIndex);
        }

        protected static int NextToken(char[] json, ref int index)
        {
            EatWhitespace(json, ref index);

            if (index == json.Length)
            {
                return JSON.TOKEN_NONE;
            }

            char c = json[index];
            index++;
            switch (c)
            {
                case '{':
                    return JSON.TOKEN_CURLY_OPEN;
                case '}':
                    return JSON.TOKEN_CURLY_CLOSE;
                case '[':
                    return JSON.TOKEN_SQUARED_OPEN;
                case ']':
                    return JSON.TOKEN_SQUARED_CLOSE;
                case ',':
                    return JSON.TOKEN_COMMA;
                case '"':
                    return JSON.TOKEN_STRING;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    return JSON.TOKEN_NUMBER;
                case ':':
                    return JSON.TOKEN_COLON;
            }
            index--;

            int remainingLength = json.Length - index;

            // false
            if (remainingLength >= 5)
            {
                if (json[index] == 'f' &&
                    json[index + 1] == 'a' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 's' &&
                    json[index + 4] == 'e')
                {
                    index += 5;
                    return JSON.TOKEN_FALSE;
                }
            }

            // true
            if (remainingLength >= 4)
            {
                if (json[index] == 't' &&
                    json[index + 1] == 'r' &&
                    json[index + 2] == 'u' &&
                    json[index + 3] == 'e')
                {
                    index += 4;
                    return JSON.TOKEN_TRUE;
                }
            }

            // null
            if (remainingLength >= 4)
            {
                if (json[index] == 'n' &&
                    json[index + 1] == 'u' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 'l')
                {
                    index += 4;
                    return JSON.TOKEN_NULL;
                }
            }

            return JSON.TOKEN_NONE;
        }
    }
}


