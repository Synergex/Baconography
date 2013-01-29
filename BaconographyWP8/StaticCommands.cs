using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyWP8.View;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BaconographyWP8
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
                    _gotoUserDetails = new RelayCommand<string>(UtilityCommandImpl.GotoUserDetails);
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
                    _gotoMarkdownLink = new RelayCommand<string>(UtilityCommandImpl.GotoLinkImpl);
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
