/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="using:Baconography.ViewModel"
                                   x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
*/

using Callisto.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using Baconography.Messages;
using Baconography.OfflineStore;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Actions;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using Baconography.View;
using System.Linq;

namespace Baconography.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<INavigationService, NavigationService>();
            SimpleIoc.Default.Register<IUsersService, UsersService>();
            SimpleIoc.Default.Register<IRedditActionQueue, RedditActionQueueService>();

            ServiceLocator.Current.GetInstance<IUsersService>().Init().ContinueWith(async (task) =>
                {
                    await ServiceLocator.Current.GetInstance<IRedditActionQueue>().Init();
                });
            
            SimpleIoc.Default.Register<RedditViewModel>();
            SimpleIoc.Default.Register<LoginViewModel>();
            SimpleIoc.Default.Register<CommentsViewModel>();
            SimpleIoc.Default.Register<LoadIndicatorView>();
            SimpleIoc.Default.Register<LinkedWebViewModel>();
            SimpleIoc.Default.Register<SubredditsViewModel>();
            SimpleIoc.Default.Register<UserDetailsViewModel>();
            SimpleIoc.Default.Register<FileOpenPickerViewModel>();
            SimpleIoc.Default.Register<SearchResultsViewModel>();
            SimpleIoc.Default.Register<ContentPreferencesViewModel>();
            SimpleIoc.Default.Register<RedditPickerViewModel>();
            SimpleIoc.Default.Register<SearchQueryViewModel>();

            //ensure we exist
            ServiceLocator.Current.GetInstance<RedditViewModel>();
            ServiceLocator.Current.GetInstance<CommentsViewModel>();
            ServiceLocator.Current.GetInstance<LinkedWebViewModel>();
            ServiceLocator.Current.GetInstance<LoginViewModel>();
            ServiceLocator.Current.GetInstance<UserDetailsViewModel>();
            ServiceLocator.Current.GetInstance<FileOpenPickerViewModel>();
            ServiceLocator.Current.GetInstance<SearchResultsViewModel>();
            ServiceLocator.Current.GetInstance<ContentPreferencesViewModel>();
            ServiceLocator.Current.GetInstance<RedditPickerViewModel>();
            ServiceLocator.Current.GetInstance<SearchQueryViewModel>();
        }

        /// <summary>
        /// Gets the Main property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public RedditViewModel Reddit
        {
            get
            {
                return ServiceLocator.Current.GetInstance<RedditViewModel>();
            }
        }

        public CommentsViewModel Comments
        {
            get
            {
                return ServiceLocator.Current.GetInstance<CommentsViewModel>();
            }
        }

        public LoadIndicatorView LoadIndicator
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LoadIndicatorView>();
            }
        }

        public LinkedWebViewModel LinkedWeb
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LinkedWebViewModel>();
            }
        }

        public LoginViewModel Login
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LoginViewModel>();
            }
        }

        public SubredditsViewModel Subreddits
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SubredditsViewModel>();
            }
        }

        public UserDetailsViewModel UserDetails
        {
            get
            {
                return ServiceLocator.Current.GetInstance<UserDetailsViewModel>();
            }
        }

        public FileOpenPickerViewModel FileOpenPicker
        {
            get
            {
                return ServiceLocator.Current.GetInstance<FileOpenPickerViewModel>();
            }
        }

        public SearchResultsViewModel SearchResults
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SearchResultsViewModel>();
            }
        }

        public SearchQueryViewModel SearchQuery
        {
            get
            {
                return ServiceLocator.Current.GetInstance<SearchQueryViewModel>();
            }
        }

        public ContentPreferencesViewModel ContentPreferences
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ContentPreferencesViewModel>();
            }
        }

        public RedditPickerViewModel RedditPicker
        {
            get
            {
                return ServiceLocator.Current.GetInstance<RedditPickerViewModel>();
            }
        }

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
                        var getAccount = new GetAccountInfo { AccountName = str };
                        var accountMessage = new SelectUserAccount { Account = await getAccount.Run() };
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                        ServiceLocator.Current.GetInstance<INavigationService>().Navigate<UserDetailsView>(accountMessage);
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
                        var imageResults = await Images.GetImagesFromUrl("", str);
                        if (imageResults != null && imageResults.Count() > 0)
                        {
                            ServiceLocator.Current.GetInstance<INavigationService>().Navigate<Baconography.View.LinkedPictureView>(imageResults);
                        }
                        else
                        {
                            //its not an image url we can understand so whatever it is just show it in the browser
                            ServiceLocator.Current.GetInstance<INavigationService>().Navigate<Baconography.View.LinkedWebView>(new NavigateToUrlMessage { TargetUrl = str, Title = str });
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
                        flyout.Content = new Baconography.View.LoginControl();
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
                        ServiceLocator.Current.GetInstance<IUsersService>().Logout();
                    });
                }
                return _doLogout;
            }
        }
    }
}