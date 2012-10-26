using GalaSoft.MvvmLight.Messaging;
using KitaroDB;
using Newtonsoft.Json;
using Baconography.Messages;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Baconography.RedditAPI
{
    public class User : INotifyPropertyChanged
    {
        private string _username;
        private string _password;
        private string _loginCookie;
        private bool _authenticated;

        internal User(string username, string password, string loginCookie, string meJson)
        {
            _username = username;
            _password = password;
            _loginCookie = loginCookie;
            _authenticated = _loginCookie != null;
            if (!string.IsNullOrWhiteSpace(meJson))
                _me = JsonConvert.DeserializeObject<Thing>(meJson).Data as Account;

            AllowOver18 = false;
            MaxTopLevelOfflineComments = 100;
            OfflineOnlyGetsFirstSet = false;
            Messenger.Default.Register<ConnectionStatusMessage>(this, (status) => IsOnline = status.IsOnline);
        }


        internal string Username
        {
            get
            {
                return _username;
            }
        }

        internal string Password
        {
            get
            {
                return _password;
            }
        }

        internal string LoginCookie
        {
            get
            {
                return _loginCookie;
            }
        }

        Account _me;

        public Account Me
        {
            get
            {
                return _me;
            }
        }

        public async Task<dynamic> UpdateMeAsync()
        {
            _me = JsonConvert.DeserializeObject<Thing>(await GetMeAsync(_username, _loginCookie)).Data as Account; ;
            return _me;
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

        public Task<string> SendPost(string data, string uri)
        {
            return SendPost(new StringContent(data), uri);
        }

        public async Task<string> SendPost(HttpContent data, string uri)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();
            var getMeClientHandler = new HttpClientHandler { CookieContainer = new CookieContainer()};
            
            if(!string.IsNullOrEmpty(_loginCookie))
                getMeClientHandler.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", _loginCookie));
            
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

        public static async Task<string> UnAuthedGet(string uri)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();
            var getClient = new HttpClient();
            getClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Baconography_Windows_8_Client", "1.0"));
            return await getClient.GetStringAsync(uri);
        }

        public async Task<string> SendGet(string uri)
        {
            //limit requests to once every 500 milliseconds
            await ThrottleRequests();
            var getMeClientHandler = new HttpClientHandler { CookieContainer = new CookieContainer() };
            
            if (!string.IsNullOrEmpty(_loginCookie))
                getMeClientHandler.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", _loginCookie));
            
            var getClient = new HttpClient(getMeClientHandler);
            getClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Baconography_Windows_8_Client", "1.0"));
            return await getClient.GetStringAsync(uri);
        }

        Task<HashSet<string>> _subscribedSubredditsTask;
        public Task<HashSet<string>> SubscribedSubreddits()
        {
            if (_subscribedSubredditsTask == null)
            {
                _subscribedSubredditsTask = SubscribedSubredditsImpl();
            }
            return _subscribedSubredditsTask;
        }

        private async Task<HashSet<string>> SubscribedSubredditsImpl()
        {
            if (Me == null)
                return new HashSet<string>();

            var subscribedSubreddits = new HashSet<string>();
            var getSubs = new Baconography.RedditAPI.Actions.GetSubscribedSubreddits();
            var subListing = await getSubs.Run(this);

            foreach (var subreddit in subListing.Data.Children)
            {
                if (subreddit.Data is Subreddit)
                {
                    subscribedSubreddits.Add(((Subreddit)subreddit.Data).Name);
                }
            }
            return subscribedSubreddits;
        }

        public static Task<string> GetMeAsync(string userName, string userCookie)
        {
            var getMeClientHandler = new HttpClientHandler { CookieContainer = new CookieContainer() };

            getMeClientHandler.CookieContainer.Add(new Uri("http://www.reddit.com", UriKind.Absolute), new Cookie("reddit_session", userCookie));
            var getMeClient = new HttpClient(getMeClientHandler);

            return getMeClient.GetStringAsync("http://www.reddit.com/api/me.json");
        }

        private static bool _isOnline = true;
        public bool IsOnline
        {
            get
            {
                return _isOnline;
            }
            set
            {
                _isOnline = value;
            }
        }

        public static void ShowDisconnectedMessage()
        {
            
            ToastTemplateType toastTemplate = ToastTemplateType.ToastText01; 
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode("We're having a hard time connecting to reddit, you might want to try again later or go into offline mode"));

            IXmlNode toastNode = toastXml.SelectSingleNode("/toast"); 
            ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\" }");

            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public bool AllowOver18 { get; set; }
        public int MaxTopLevelOfflineComments { get; set; }
        public bool OfflineOnlyGetsFirstSet { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
