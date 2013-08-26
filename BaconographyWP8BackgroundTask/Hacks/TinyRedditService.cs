﻿using BaconographyWP8;
using Procurios.Public;
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

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

        public async Task<Stream> CacheUrl(string url)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(url);
            request.Method = "GET";
            request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";

            var getResult = await GetResponseAsync(request);
            if (getResult.StatusCode == HttpStatusCode.OK && (getResult.ContentLength < 1024 * 256 || getResult.ContentLength == 4294967295))
            {
                if (getResult.StatusCode == HttpStatusCode.OK)
                {
                    return getResult.GetResponseStream();
                }
            }

            if (getResult != null)
                getResult.Dispose();

            return null;
        }

        public async Task<bool> SetSourceUrl(string url, BitmapImage image)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(url);
            request.AllowReadStreamBuffering = true;
            request.Method = "GET";
            request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";

            using (var getResult = await GetResponseAsync(request))
            {
                if (getResult.StatusCode == HttpStatusCode.OK && (getResult.ContentLength < 1024 * 256 || getResult.ContentLength == 4294967295))
                {
                    using (var responseStream = getResult.GetResponseStream())
                    {
                        var dimensions = BackgroundTask.GetJpegDimensions(responseStream);
                        responseStream.Seek(0, SeekOrigin.Begin);
                        if (dimensions == null || (dimensions.Height * dimensions.Width * 4) > (1024 * 1536))
                        {
                            return false;
                        }
                        else
                        {
                            try
                            {
                                image.SetSource(responseStream);
                            }
                            catch
                            {
                                //force bad debug traces to know we were here
                                throw;
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public async Task<string> SendGet(string cookie, string uri)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";
            request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";
            request.AllowReadStreamBuffering = true;
            var cookieContainer = new CookieContainer();
            request.CookieContainer = cookieContainer;

            if (!string.IsNullOrEmpty(cookie))
                request.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", cookie));

            using (var getResult = await GetResponseAsync(request))
            {
                if (getResult.StatusCode == HttpStatusCode.OK)
                {
                    using (var responseStream = getResult.GetResponseStream())
                    {
                        using (var sr = new StreamReader(responseStream))
                        {
                            var result = sr.ReadToEnd();
                            return result;
                        }
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

                var hasMail = JSON.GetValue(data, "has_mail") as Nullable<bool>;
                var hasModMail = JSON.GetValue(data, "has_mod_mail") as Nullable<bool>;


                return (hasMail ?? false) || (hasModMail ?? false);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<Tuple<IEnumerable<string>, int>> GetNewMessages(int? limit)
        {
            var targetUri = "http://www.reddit.com/message/unread/.json";

            try
            {
                HashSet<string> existingMessages = new HashSet<string>();
                if (File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "bgtaskMessages.txt"))
                {
                    using (var bgTaskToastedMessages = File.OpenText(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "bgtaskMessages.txt"))
                    {
                        while (!bgTaskToastedMessages.EndOfStream)
                        {
                            var msgId = bgTaskToastedMessages.ReadLine();
                            if(!existingMessages.Contains(msgId))
                                existingMessages.Add(msgId);
                        }
                    }

                }

                var result = new List<string>();
                var messages = await SendGet(_loginCookie, targetUri);
                var decodedJson = JSON.JsonDecode(messages);
                var children = JSON.GetValue(JSON.GetValue(decodedJson, "data"), "children") as List<object>;
                var messageNames = new List<string>();
                int newMessages = 0;
                foreach (var child in children)
                {
                    var data = JSON.GetValue(child, "data");
                    var isNew = JSON.GetValue(data, "new") as Nullable<bool>;
                    var name = JSON.GetValue(data, "name") as string;

                    if (isNew ?? false)
                    {
                        newMessages++;

                        if (existingMessages.Contains(name))
                            continue;

                        messageNames.Add(name);
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

                if (result.Count > 0)
                {
                    using (var bgTaskToastedMessages = File.CreateText(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "bgtaskMessages.txt"))
                    {
                        foreach(var messageName in messageNames)
                        {
                            bgTaskToastedMessages.WriteLine(messageName);
                        }
                    }
                }

                return Tuple.Create<IEnumerable<string>, int>(result, newMessages);
            }
            catch (Exception ex)
            {
                return Tuple.Create(Enumerable.Empty<string>(), 0);
            }
        }

        public async Task<IEnumerable<Tuple<string, string>>> GetPostsBySubreddit(string subreddit, int? limit)
        {
            if (subreddit == null)
            {
                return Enumerable.Empty<Tuple<string, string>>();
            }

            var targetUri = string.Format("http://www.reddit.com{0}.json?limit={1}", subreddit, limit ?? 25);
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
                    var nsfw = JSON.GetValue(data, "over_18") as Nullable<bool>;
                    if (nsfw ?? false)
                        continue;
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
