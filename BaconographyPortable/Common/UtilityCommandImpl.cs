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
        public static async void GotoUserDetails(string str)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var getAccount = await ServiceLocator.Current.GetInstance<IBaconProvider>().GetService<IRedditService>().GetAccountInfo(str);
            var accountMessage = new SelectUserAccountMessage { Account = getAccount };
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            ServiceLocator.Current.GetInstance<INavigationService>().Navigate(ServiceLocator.Current.GetInstance<IDynamicViewLocator>().AboutUserView,accountMessage);
        }

        //Subreddit:
        private static Regex _subredditRegex = new Regex("/r/[a-zA-Z0-9_]+/?");

        //Comments page:
        private static Regex _commentsPageRegex = new Regex("/r/[a-zA-Z0-9_]+/comments/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/?");

        //Comment:
        private static Regex _commentRegex = new Regex("/r/[a-zA-Z0-9_]+/comments/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/?");

        public static async void GotoLinkImpl(string str)
        {
            var baconProvider = ServiceLocator.Current.GetInstance<IBaconProvider>();
            var navigationService = baconProvider.GetService<INavigationService>();
            

            if (_commentsPageRegex.IsMatch(str) && !_commentRegex.IsMatch(str))
            {
                var targetLinkThing = await baconProvider.GetService<IRedditService>().GetLinkByUrl(str);
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
            else
            {
                await baconProvider.GetService<IOfflineService>().StoreHistory(str);
                var imageResults = await baconProvider.GetService<IImagesService>().GetImagesFromUrl("", str);
                if (imageResults != null && imageResults.Count() > 0)
                {
                    navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().LinkedPictureView, imageResults);
                }
                else
                {
                    //its not an image url we can understand so whatever it is just show it in the browser
                    navigationService.Navigate(baconProvider.GetService<IDynamicViewLocator>().LinkedWebView, new NavigateToUrlMessage { TargetUrl = str, Title = str });
                }
            }
        }
    }
}
