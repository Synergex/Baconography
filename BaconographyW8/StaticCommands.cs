﻿using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyW8.Common;
using BaconographyW8.View;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BaconographyW8
{
    public class StaticCommands
    {
        private RelayCommand<string> _gotoUserDetails;
        public RelayCommand<string> GotoUserDetails
        {
            get
            {
                if (_gotoUserDetails == null)
                {
                    _gotoUserDetails = new RelayCommand<string>(async (str) =>
                    {
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                        var getAccount =  await ServiceLocator.Current.GetInstance<IBaconProvider>().GetService<IRedditService>().GetAccountInfo(str);
                        var accountMessage = new SelectUserAccountMessage { Account = getAccount};
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                        ServiceLocator.Current.GetInstance<INavigationService>().Navigate<AboutUserView>(accountMessage);
                    });
                }
                return _gotoUserDetails;
            }
        }

        //Subreddit:
        private Regex _subredditRegex = new Regex("/r/[a-zA-Z0-9_]+/?");

        //Comments page:
        private Regex _commentsPageRegex = new Regex("/r/[a-zA-Z0-9_]+/comments/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/?");

        //Comment:
        private Regex _commentRegex = new Regex("/r/[a-zA-Z0-9_]+/comments/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/[a-zA-Z0-9_]+/?");

        private RelayCommand<string> _gotoMarkdownLink;
        public RelayCommand<string> GotoMarkdownLink
        {
            get
            {
                if (_gotoMarkdownLink == null)
                {
                    _gotoMarkdownLink = new RelayCommand<string>(GotoMarkdownLinkImpl);
                }
                return _gotoMarkdownLink;
            }
        }

        private async void GotoMarkdownLinkImpl(string str)
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
                    navigationService.Navigate<LinkedWebView>(new NavigateToUrlMessage { TargetUrl = str, Title = str });
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
                    if(thing != null)
                        subreddit = new TypedThing<Subreddit>(thing);
                }

                if(subreddit != null)
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
                    navigationService.Navigate<LinkedPictureView>(imageResults);
                }
                else
                {
                    //its not an image url we can understand so whatever it is just show it in the browser
                    navigationService.Navigate<LinkedWebView>(new NavigateToUrlMessage { TargetUrl = str, Title = str });
                }
            }
        }

        bool _isTypeToSearch = false;
        RelayCommand _showLogin;
        public RelayCommand ShowLogin
        {
            get
            {
                if (_showLogin == null)
                {
                    _showLogin = new RelayCommand(() =>
                    {
                        var flyout = new SettingsFlyout();
                        flyout.Content = new LoginView();
                        flyout.HeaderText = "Login";
                        flyout.IsOpen = true;
                        flyout.Closed += (e, sender) =>
                        {
                            Messenger.Default.Unregister<CloseSettingsMessage>(this);
                            App.SetSearchKeyboard(_isTypeToSearch);
                        };
                        Messenger.Default.Register<CloseSettingsMessage>(this, (message) =>
                        {
                            flyout.IsOpen = false;
                            App.SetSearchKeyboard(_isTypeToSearch);
                        });

                        _isTypeToSearch = App.GetSearchKeyboard();
                        App.SetSearchKeyboard(false);
                    });
                }
                return _showLogin;
            }
        }

        RelayCommand _doLogout;
        public RelayCommand DoLogout
        {
            get
            {
                if (_doLogout == null)
                {
                    _doLogout = new RelayCommand(() =>
                    {
                        ServiceLocator.Current.GetInstance<IBaconProvider>().GetService<IUserService>().Logout();
                    });
                }
                return _doLogout;
            }
        }
    }
}
