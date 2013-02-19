using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.PlatformServices
{
    class SimpleHttpService : ISimpleHttpService
    {
        public Task<string> SendPost(string cookie, Dictionary<string, string> urlEncodedData, string uri)
        {
            var stringData = string.Join("&", urlEncodedData.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)));

            return SendPost(cookie, Encoding.UTF8.GetBytes(stringData), uri, "application/x-www-form-urlencoded");
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
            request.BeginGetResponse(asyncResponse =>
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

        private async Task<string> SendPost(string cookie, byte[] data, string uri, string contentType)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "POST";
            request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";
            request.ContentType = contentType;
            request.ContentLength = data.Length;

            using (var requestStream = (await GetRequestStreamAsync(request))) { requestStream.Write(data, 0, data.Length); }

            if (!string.IsNullOrEmpty(cookie))
                request.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", cookie));

            var postResult = await GetResponseAsync(request);

            if (postResult.StatusCode == HttpStatusCode.OK)
            {
                return await Task<string>.Run(() =>
                {
                    using (var sr = new StreamReader(postResult.GetResponseStream()))
                    {
                        return sr.ReadToEnd();
                    }
                });
            }
            else
                throw new Exception(postResult.StatusCode.ToString());
        }

        public async Task<string> SendGet(string cookie, string uri)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";
            request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";

            if (!string.IsNullOrEmpty(cookie))
                request.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", cookie));

            var getResult = await GetResponseAsync(request);

            if (getResult.StatusCode == HttpStatusCode.OK)
            {
                return await Task<string>.Run(() =>
                    {
                        using (var sr = new StreamReader(getResult.GetResponseStream()))
                        {
                            return sr.ReadToEnd();
                        }
                    });
            }
            else
                throw new Exception(getResult.StatusCode.ToString());
        }

        public Task<Tuple<string, Dictionary<string, string>>> SendPostForCookies(Dictionary<string, string> urlEncodedData, string uri)
        {
            //TODO: implement me
            throw new NotImplementedException();
        }

        public async Task<string> UnAuthedGet(string uri)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "GET";
            request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";

            var getResult = await GetResponseAsync(request);

            if (getResult.StatusCode == HttpStatusCode.OK)
            {
                return await Task<string>.Run(() =>
                {
                    using (var sr = new StreamReader(getResult.GetResponseStream()))
                    {
                        return sr.ReadToEnd();
                    }
                });
            }
            else
                throw new Exception(getResult.StatusCode.ToString());
        }

        static DateTime _priorRequestSet = new DateTime();
        static int _requestSetCount = 0;
        static DateTime _lastRequestMade = new DateTime();

        //dont hammer reddit!
        public static async Task ThrottleRequests()
        {
            var offset = DateTime.Now - _lastRequestMade;
            if (offset.TotalMilliseconds < 2000)
            {
                await Task.Delay(2000 - (int)offset.TotalMilliseconds);
            }

            if (_requestSetCount > 30)
            {
                var overallOffset = DateTime.Now - _priorRequestSet;

                if (overallOffset.TotalSeconds < 60)
                {
                    await Task.Delay((60 - (int)overallOffset.TotalSeconds) * 1000);
                    _requestSetCount = 0;
                    _priorRequestSet = DateTime.Now;
                }
            }
            _requestSetCount++;

            _lastRequestMade = DateTime.Now;
        }
    }
}
