using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyW8.PlatformServices
{
    class SimpleHttpService : ISimpleHttpService
    {
        public Task<string> SendPost(string cookie, string data, string uri)
        {
            return SendPost(cookie, new StringContent(data), uri);
        }

        public Task<string> SendPost(string cookie, Dictionary<string, string> urlEncodedData, string uri)
        {
            return SendPost(cookie, new FormUrlEncodedContent(urlEncodedData), uri);
        }

        private async Task<string> SendPost(string cookie, HttpContent data, string uri)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();
            var getMeClientHandler = new HttpClientHandler { CookieContainer = new CookieContainer() };

            if (!string.IsNullOrEmpty(cookie))
                getMeClientHandler.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", cookie));

            var postClient = new HttpClient(getMeClientHandler);
            postClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Baconography_Windows_8_Client", "1.0"));
            var postResult = await postClient.PostAsync(uri, data);

            if (postResult.IsSuccessStatusCode)
            {
                return await postResult.Content.ReadAsStringAsync();
            }
            else
                throw new Exception(postResult.StatusCode.ToString());
        }

        public async Task<string> SendGet(string cookie, string uri)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();
            var getMeClientHandler = new HttpClientHandler { CookieContainer = new CookieContainer() };

            if (!string.IsNullOrEmpty(cookie))
                getMeClientHandler.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", cookie));

            var getClient = new HttpClient(getMeClientHandler);
            getClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Baconography_Windows_8_Client", "1.0"));
            return await getClient.GetStringAsync(uri);
        }

        public async Task<Tuple<string, Dictionary<string, string>>> SendPostForCookies(Dictionary<string, string> urlEncodedData, string uri)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();
            var getMeClientHandler = new HttpClientHandler { CookieContainer = new CookieContainer() };

            var postClient = new HttpClient(getMeClientHandler);
            postClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Baconography_Windows_8_Client", "1.0"));
            var postResult = await postClient.PostAsync(uri, new FormUrlEncodedContent(urlEncodedData));

            if (postResult.IsSuccessStatusCode)
            {
                var jsonResult = await postResult.Content.ReadAsStringAsync();

                var loginCookies = getMeClientHandler.CookieContainer.GetCookies(new Uri(uri));
                var loginCookie = loginCookies["reddit_session"].Value;

                return Tuple.Create(jsonResult, new Dictionary<string, string> { {"reddit_session", loginCookie} });
            }
            else
                throw new Exception(postResult.StatusCode.ToString());
        }

        public async Task<string> UnAuthedGet(string uri)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();
            var getClient = new HttpClient();
            getClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Baconography_Windows_8_Client", "1.0"));
            return await getClient.GetStringAsync(uri);
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
