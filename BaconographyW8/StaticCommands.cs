using BaconographyPortable.Messages;
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

        private RelayCommand<string> _gotoMarkdownLink;
        public RelayCommand<string> GotoMarkdownLink
        {
            get
            {
                if (_gotoMarkdownLink == null)
                {
                    _gotoMarkdownLink = new RelayCommand<string>(async (str) =>
                    {
                        

                        var baconProvider = ServiceLocator.Current.GetInstance<IBaconProvider>();
                        await baconProvider.GetService<IOfflineService>().StoreHistory(str);
                        var imageResults = await baconProvider.GetService<IImagesService>().GetImagesFromUrl("", str);
                        if (imageResults != null && imageResults.Count() > 0)
                        {
                            ServiceLocator.Current.GetInstance<INavigationService>().Navigate<LinkedPictureView>(imageResults);
                        }
                        else
                        {
                            //its not an image url we can understand so whatever it is just show it in the browser
                            ServiceLocator.Current.GetInstance<INavigationService>().Navigate<LinkedWebView>(new NavigateToUrlMessage { TargetUrl = str, Title = str });
                        }
                    });
                }
                return _gotoMarkdownLink;
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
