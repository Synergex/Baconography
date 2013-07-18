using Procurios.Public;
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8BackgroundTask.Hacks
{
    class TinyRedditService
    {
        string _username;
        string _loginCookie;
        string _password;
        public TinyRedditService(string username, string password, string loginCookie)
        {
            _username = username;
            _password = password;
            _loginCookie = loginCookie;
        }

        public static Task<HttpWebResponse> GetResponseAsync(HttpWebRequest request)
        {
            var taskComplete = new TaskCompletionSource<HttpWebResponse>();
            request.BeginGetResponse(asyncResponse =>
            {
                try
                {
                    HttpWebRequest responseRequest = (HttpWebRequest)asyncResponse.AsyncState;
                    HttpWebResponse someResponse = (HttpWebResponse)responseRequest.EndGetResponse(asyncResponse);
                    taskComplete.TrySetResult(someResponse);
                }
                catch (WebException webExc)
                {
                    HttpWebResponse failedResponse = (HttpWebResponse)webExc.Response;
                    taskComplete.TrySetResult(failedResponse);
                }
            }, request);
            return taskComplete.Task;
        }

        public static Task<Stream> GetRequestStreamAsync(HttpWebRequest request)
        {
            var taskComplete = new TaskCompletionSource<Stream>();
            request.BeginGetRequestStream(asyncResponse =>
            {
                try
                {
                    HttpWebRequest responseRequest = (HttpWebRequest)asyncResponse.AsyncState;
                    Stream someResponse = (Stream)responseRequest.EndGetRequestStream(asyncResponse);
                    taskComplete.TrySetResult(someResponse);
                }
                catch (Exception)
                {
                    taskComplete.TrySetResult(null);
                }
            }, request);
            return taskComplete.Task;
        }

        public async Task<string> SendGet(string cookie, string uri)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.AllowReadStreamBuffering = false;
            request.AllowWriteStreamBuffering = false;
            request.Method = "GET";
            request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";
            var cookieContainer = new CookieContainer();
            request.CookieContainer = cookieContainer;

            if (!string.IsNullOrEmpty(cookie))
                request.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", cookie));

            using (var getResult = await GetResponseAsync(request))
            {
                if (getResult.StatusCode == HttpStatusCode.OK)
                {
                    using (var sr = new StreamReader(getResult.GetResponseStream()))
                    {
                        var result = sr.ReadToEnd();
                        return result;
                    }
                }
            }
            return "";
        }

        public async Task<bool> HasMail()
        {
            try
            {
                var meString = await SendGet(_loginCookie, "http://www.reddit.com/api/me.json");
                var decodedJson = JSON.JsonDecode(meString);
                var data = JSON.GetValue(decodedJson, "data");

                var hasMail = JSON.GetValue(decodedJson, "has_mail") as Nullable<bool>;
                var hasModMail = JSON.GetValue(decodedJson, "has_mod_mail") as Nullable<bool>;


                return (hasMail ?? false) || (hasModMail ?? false);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetNewMessages(int? limit)
        {
            var targetUri = "http://www.reddit.com/message/inbox/.json";

            try
            {
                var result = new List<string>();
                var messages = await SendGet(_loginCookie, targetUri);
                var decodedJson = JSON.JsonDecode(messages);
                var children = JSON.GetValue(JSON.GetValue(decodedJson, "data"), "children") as List<object>;

                foreach (var child in children)
                {
                    var data = JSON.GetValue(child, "data");
                    var isNew = JSON.GetValue(data, "new") as Nullable<bool>;
                    if (isNew ?? false)
                    {
                        var subject = JSON.GetValue(data, "subject") as string;
                        var wasComment = JSON.GetValue(data, "was_comment") as Nullable<bool>;
                        if (wasComment ?? false)
                        {
                            var linkTitle = JSON.GetValue(data, "link_title") as string;
                            result.Add(linkTitle);
                        }
                        else
                        {
                            result.Add(subject);
                        }
                    }
                    
                }
                return result;
            }
            catch (Exception ex)
            {
                return Enumerable.Empty<string>();
            }
        }

        public async Task<IEnumerable<Tuple<string, string>>> GetPostsBySubreddit(string subreddit, int? limit)
        {
            if (subreddit == null)
            {
                return Enumerable.Empty<Tuple<string, string>>();
            }

            var targetUri = string.Format("http://www.reddit.com{0}.json", subreddit);
            try
            {
                List<Tuple<string, string>> result = new List<Tuple<string, string>>();
                var links = await SendGet(_loginCookie, targetUri);
                var decodedJson = JSON.JsonDecode(links);
                var children = JSON.GetValue(JSON.GetValue(decodedJson, "data"), "children") as List<object>;

                foreach (var child in children)
                {
                    var data = JSON.GetValue(child, "data");
                    var isSelf = JSON.GetValue(data, "is_self") as Nullable<bool>;
                    var url = JSON.GetValue(data, "url") as string;
                    var title = JSON.GetValue(data, "title") as string;
                    result.Add(Tuple.Create(title, (isSelf ?? false) ? "" : url)); 
                }

                return result;
            }
            catch (Exception ex)
            {
                return Enumerable.Empty<Tuple<string, string>>();
            }
        }
    }
}
