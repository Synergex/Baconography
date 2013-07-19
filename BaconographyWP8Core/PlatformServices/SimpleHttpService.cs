using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace BaconographyWP8.PlatformServices
{
    public class SimpleHttpService : ISimpleHttpService
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

        private async Task<string> SendPost(string cookie, byte[] data, string uri, string contentType)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Method = "POST";
            request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";
            request.ContentType = contentType;
            request.ContentLength = data.Length;
			request.CookieContainer = new CookieContainer();
			if (!string.IsNullOrEmpty(cookie))
				request.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", cookie));

            using (var requestStream = (await GetRequestStreamAsync(request))) { requestStream.Write(data, 0, data.Length); }

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

        private async Task<string> SendGet(string cookie, string uri, bool hasRetried)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();

            HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
            request.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();
            request.Method = "GET";
            request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";
            var cookieContainer = new CookieContainer();
            request.CookieContainer = cookieContainer;

            if (!string.IsNullOrEmpty(cookie))
                request.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", cookie));

            var getResult = await GetResponseAsync(request);

            if (getResult != null && getResult.StatusCode == HttpStatusCode.OK)
            {
                return await Task<string>.Run(() =>
                {
                    using (var sr = new StreamReader(getResult.GetResponseStream()))
                    {
                        var result = sr.ReadToEnd();
                        return result;
                    }
                });
            }
            else if (!hasRetried)
            {
                int networkDownRetries = 0;
                while (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() && networkDownRetries < 10)
                {
                    networkDownRetries++;
                    await Task.Delay(1000);
                }
                   
                return await SendGet(cookie, uri, true);
            }
            else
            {
                throw new Exception(getResult.StatusCode.ToString());
            }
        }
        public Task<string> SendGet(string cookie, string uri)
        {
            return SendGet(cookie, uri, false); 
        }

        public async Task<Tuple<string, Dictionary<string, string>>> SendPostForCookies(Dictionary<string, string> urlEncodedData, string uri)
        {
			//limit requests to once every 500 milliseconds
			await ThrottleRequests();

			StringBuilder dataBuilder = new StringBuilder();
			var stringData = string.Join("&", urlEncodedData.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)));
			byte[] data = Encoding.UTF8.GetBytes(stringData);

			HttpWebRequest request = HttpWebRequest.CreateHttp(uri);
			request.Method = "POST";
			request.UserAgent = "Baconography_Windows_Phone_8_Client/1.0";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = data.Length;
			var container = new CookieContainer();
			request.CookieContainer = container;
			//request.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", "", "/", "reddit.com"));

			using (var requestStream = (await GetRequestStreamAsync(request))) { requestStream.Write(data, 0, data.Length); }

			var postResult = await GetResponseAsync(request);

			if (postResult.StatusCode == HttpStatusCode.OK)
			{
				return await Task<Tuple<string, Dictionary<string, string>>>.Run(() =>
				{
					using (var sr = new StreamReader(postResult.GetResponseStream()))
					{
						container.GetCookies(new Uri("http://www.reddit.com", UriKind.Absolute));
						string loginCookie = "";
						var jsonResult = sr.ReadToEnd();
						var loginResultThing = JsonConvert.DeserializeObject<LoginJsonThing>(jsonResult);
						if (loginResultThing != null && loginResultThing.Json != null &&
							(loginResultThing.Json.Errors == null || loginResultThing.Json.Errors.Length == 0))
						{
							loginCookie = HttpUtility.UrlEncode(loginResultThing.Json.Data.Cookie);
						}
						if (!String.IsNullOrEmpty(loginCookie))
							return Tuple.Create(jsonResult, new Dictionary<string, string> { { "reddit_session", loginCookie } });
						else
							return Tuple.Create<string, Dictionary<string, string>>(jsonResult, null);
					}
				});
			}
			else
				throw new Exception(postResult.StatusCode.ToString());
        }

        private async Task<string> UnAuthedGet(string uri, bool hasRetried)
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
            else if (!hasRetried)
            {
                int networkDownRetries = 0;
                while (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() && networkDownRetries < 10)
                {
                    networkDownRetries++;
                    await Task.Delay(1000);
                }

                return await UnAuthedGet(uri, true);
            }
            else
                throw new Exception(getResult.StatusCode.ToString());
        }
        public Task<string> UnAuthedGet(string uri)
        {
            return UnAuthedGet(uri, false);
        }

        static DateTime _priorRequestSet = new DateTime();
        static int _requestSetCount = 0;
        static DateTime _lastRequestMade = new DateTime();

        //dont hammer reddit!
        //Make no more than thirty requests per minute. This allows some burstiness to your requests, 
        //but keep it sane. On average, we should see no more than one request every two seconds from you.
        //the above statement is from the reddit api docs, but its not quite true, there are some api's that have logging 
        //set for 15 requests in 30 seconds, so we can allow some burstiness but it must fit in the 15 requests/30 seconds rule
        public static async Task ThrottleRequests()
        {
            var offset = DateTime.Now - _lastRequestMade;
            if (offset.TotalMilliseconds < 1000)
            {
                await Task.Delay(1000 - (int)offset.TotalMilliseconds);
            }

            if (_requestSetCount > 15)
            {
                var overallOffset = DateTime.Now - _priorRequestSet;

                if (overallOffset.TotalSeconds < 30)
                {
                    await Task.Delay((30 - (int)overallOffset.TotalSeconds) * 1000);
                }
                _requestSetCount = 0;
                _priorRequestSet = DateTime.Now;
            }
            _requestSetCount++;

            _lastRequestMade = DateTime.Now;
        }
    }
}
