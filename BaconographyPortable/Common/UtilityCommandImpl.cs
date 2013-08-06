using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BaconographyPortable.Common
{
    public class UtilityCommandImpl
    {
        private class LongNavWatcher
        {
            private string _inFlight;
            public void WatchMessage(LongNavigationMessage message)
            {
                if(!message.Finished)
                {
                    lock (this)
                    {
                        _inFlight = message.TargetUrl;
                    }
                }
                else
                {
                    lock (this)
                    {
                        _inFlight = null;
                    }
                }
            }
            public bool GetTerminatedClearInFlight(string url)
            {
                lock (this)
                {
                    if (_inFlight == url)
                    {
                        _inFlight = null;
                        return false;
                    }
                }
                return true;
            }

            public void ClearInFlight()
            {
                lock (this)
                {
                    _inFlight = null;
                }
            }
        }
        private static LongNavWatcher _longNavWatcher = new LongNavWatcher();
        static UtilityCommandImpl()
        {
            Messenger.Default.Register<LongNavigationMessage>(_longNavWatcher, _longNavWatcher.WatchMessage);
        }

        public static async void GotoUserDetails(string str)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var getAccount = await ServiceLocator.Current.GetInstance<IBaconProvider>().GetService<IRedditService>().GetAccountInfo(str);
            var accountMessage = new SelectUserAccountMessage { Account = getAccount };
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            ServiceLocator.Current.GetInstance<INavigationService>().Navigate(ServiceLocator.Current.GetInstance<IDynamicViewLocator>().AboutUserView,accountMessage);
        }

        //Subreddit:
		private static Regex _subredditRegex = new Regex("(?:^|\\s|reddit.com)/r/[a-zA-Z0-9_]+/?");

        //Comments page:
		private static Regex _commentsPageRegex = new Regex("(?:^|\\s|reddit.com)/r/[a-zA-Z0-9_]+/comments/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/?");

        //Comment:
		private static Regex _commentRegex = new Regex("(?:^|\\s|reddit.com)/r/[a-zA-Z0-9_]+/comments/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/?");

        //User Multireddit:
        private static Regex _userMultiredditRegex = new Regex("(?:^|\\s|reddit.com)/u(?:ser)*/[a-zA-Z0-9_]+/m/[a-zA-Z0-9_]+/?");

		//User:
		private static Regex _userRegex = new Regex("(?:^|\\s|reddit.com)/u(?:ser)*/[a-zA-Z0-9_]+/?");

        public static void GotoLinkImpl(string str)
        {
            GotoLinkImpl(str, null);
        }

        public static async void GotoLinkImpl(string str, TypedThing<Link> sourceLink)
        {
            _longNavWatcher.ClearInFlight();
            var baconProvider = ServiceLocator.Current.GetInstance<IBaconProvider>();
            var navigationService = baconProvider.GetService<INavigationService>();

            if (_commentRegex.IsMatch(str))
            {
                var typedLinkThing = new TypedThing<Link>(new Thing { Kind = "t3", Data = new Link { Permalink = str } });
                navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().CommentsView, new SelectCommentTreeMessage { LinkThing = typedLinkThing });
            }
            else if (_commentsPageRegex.IsMatch(str))
            {
                var targetLinkThing = sourceLink == null ? await baconProvider.GetService<IRedditService>().GetLinkByUrl(str) : sourceLink;
                if (targetLinkThing != null)
                {
                    var typedLinkThing = new TypedThing<Link>(targetLinkThing);
                    await baconProvider.GetService<IOfflineService>().StoreHistory(typedLinkThing.Data.Permalink);
                    navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().CommentsView, new SelectCommentTreeMessage { LinkThing = typedLinkThing });
                }
                else
                {
                    navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().LinkedWebView, new NavigateToUrlMessage { TargetUrl = str, Title = str });
                }
            }
            else if (_subredditRegex.IsMatch(str))
            {
                var nameIndex = str.LastIndexOf("/r/");
                var subredditName = str.Substring(nameIndex + 3);

                TypedThing<Subreddit> subreddit = null;

                var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
                var offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();
                if (settingsService.IsOnline())
                {
                    subreddit = await baconProvider.GetService<IRedditService>().GetSubreddit(subredditName);
                }
                else
                {
                    var thing = await offlineService.GetSubreddit(subredditName);
                    if (thing != null)
                        subreddit = new TypedThing<Subreddit>(thing);
                }

                if (subreddit != null)
                    navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().RedditView, new SelectSubredditMessage { Subreddit = subreddit });
                else
                    ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("This subreddit is not available in offline mode");
            }
            else if (_userMultiredditRegex.IsMatch(str))
            {
                var nameIndex = str.LastIndexOf("/u/");
                string subredditName = "";
                if (nameIndex < 0)
                {
                    nameIndex = str.LastIndexOf("/user/");
                    subredditName = str.Substring(nameIndex);
                }
                else
                {
                    subredditName = str.Substring(nameIndex);
                }

                subredditName = subredditName.Replace("/u/", "/user/");

                TypedThing<Subreddit> subreddit = null;

                var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
                var offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();
                if (settingsService.IsOnline())
                {
                    subreddit = await baconProvider.GetService<IRedditService>().GetSubreddit(subredditName);
                }
                else
                {
                    var thing = await offlineService.GetSubreddit(subredditName);
                    if (thing != null)
                        subreddit = new TypedThing<Subreddit>(thing);
                }

                if (subreddit != null)
                    navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().RedditView, new SelectSubredditMessage { Subreddit = subreddit });
                else
                    ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("This subreddit is not available in offline mode");
            }
			else if (_userRegex.IsMatch(str))
			{
				var nameIndex = str.LastIndexOf("/u/");
				string userName = "";
				if (nameIndex < 0)
				{
					nameIndex = str.LastIndexOf("/user/");
					userName = str.Substring(nameIndex + 6);
				}
				else
				{
					userName = str.Substring(nameIndex + 3);
				}

				TypedThing<Account> account = null;

				var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
				var offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();
				if (settingsService.IsOnline())
				{
					account = await baconProvider.GetService<IRedditService>().GetAccountInfo(userName);

					if (account != null)
						navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().AboutUserView, new SelectUserAccountMessage { Account = account });
					else
						ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("This account does not exist.");
				}
				else
				{
					ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("Cannot access user info in offline mode.");
				}				
			}
			else
			{
                var smartOfflineService = baconProvider.GetService<ISmartOfflineService>();
                smartOfflineService.NavigatedToOfflineableThing(sourceLink, false);
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                Messenger.Default.Send<LongNavigationMessage>(new LongNavigationMessage { Finished = false, TargetUrl = str });
				await baconProvider.GetService<IOfflineService>().StoreHistory(str);
                var imageResults = await baconProvider.GetService<IImagesService>().GetImagesFromUrl(sourceLink == null ? "" : sourceLink.Data.Title, str);
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                
				if (imageResults != null && imageResults.Count() > 0 && !_longNavWatcher.GetTerminatedClearInFlight(str))
				{
                    var imageTuple = new Tuple<string, IEnumerable<Tuple<string, string>>>(sourceLink != null ? sourceLink.Data.Title : "", imageResults);
                    Messenger.Default.Send<LongNavigationMessage>(new LongNavigationMessage { Finished = true, TargetUrl = str });
                    navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().LinkedPictureView, imageTuple);
				}
				else
				{
                    Messenger.Default.Send<LongNavigationMessage>(new LongNavigationMessage { Finished = true, TargetUrl = str });
					var videoResults = await baconProvider.GetService<IVideoService>().GetPlayableStreams(str);
                    if (videoResults != null)
                    {
                        navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().LinkedVideoView, videoResults);
                    }
                    else
                    {
                        //its not an image/video url we can understand so whatever it is just show it in the browser
                        navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().LinkedWebView, new NavigateToUrlMessage { TargetUrl = str, Title = str });
                    }
				}
			}
        }
    }
}
