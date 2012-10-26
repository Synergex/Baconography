using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Baconography.ViewModel
{
    public class LoginViewModel : ViewModelBase
    {
        IUsersService _userService;
        public LoginViewModel(IUsersService userService)
        {
            _userService = userService;
            _isLoggedIn = false;
            MessengerInstance.Register<UserLoggedIn>(this, (userMessage) =>
                {
                    if (userMessage != null && userMessage.CurrentUser != null && !string.IsNullOrWhiteSpace(userMessage.CurrentUser.Username))
                    {
                        CurrentUserName = userMessage.CurrentUser.Username;
                        IsLoggedIn = true;
                    }
                    else
                    {
                        CurrentUserName = string.Empty;
                        IsLoggedIn = false;
                    }
                });
        }

        string _username;
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
                RaisePropertyChanged("Username");
            }
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get
            {
                return _isLoggedIn;
            }
            set
            {
                _isLoggedIn = value;
                RaisePropertyChanged("IsLoggedIn");
            }
        }

        private string _currentUserName;
        public string CurrentUserName
        {
            get
            {
                return _currentUserName;
            }
            set
            {
                _currentUserName = value;
                RaisePropertyChanged("CurrentUserName");
            }
        }

        string _password;
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                RaisePropertyChanged("Password");
            }
        }

        private bool _hasErrors = false;
        public bool HasErrors
        {
            get
            {
                return _hasErrors;
            }
            set
            {
                _hasErrors = value;
                RaisePropertyChanged("HasErrors");
            }
        }

        private bool _working = false;
        public bool Working
        {
            get
            {
                return _working;
            }
            set
            {
                _working = value;
                RaisePropertyChanged("Working");
            }
        }

        private bool _isDefaultLogin;
        public bool IsDefaultLogin
        {
            get
            {
                return _isDefaultLogin;
            }
            set
            {
                _isDefaultLogin = value;
                RaisePropertyChanged("IsDefaultLogin");
            }
        }

        private bool _isRememberLogin;
        public bool IsRememberLogin
        {
            get
            {
                return _isRememberLogin;
            }
            set
            {
                _isRememberLogin = value;
                RaisePropertyChanged("IsRememberLogin");
            }
        }

        RelayCommand _doLogin;
        public RelayCommand DoLogin
        {
            get
            {
                if (_doLogin == null)
                {
                    _doLogin = new RelayCommand(async () =>
                        {
                            Working = true;
                            var loggedInUser = await _userService.TryLogin(Username, Password);
                            if (loggedInUser == null)
                            {
                                HasErrors = true;
                                Working = false;
                            }
                            else
                            {
                                HasErrors = false;
                                Working = false;
                                if (IsRememberLogin)
                                {
                                    var newCredentials = new UserCredential
                                    {
                                        IsDefault = IsDefaultLogin,
                                        LoginCookie = loggedInUser.LoginCookie,
                                        Username = loggedInUser.Username,
                                        Me = new Thing { Kind = "t2", Data = loggedInUser.Me }
                                    };
                                    await _userService.AddStoredCredential(newCredentials, loggedInUser.Password);
                                    //reload credentials
                                    _credentials = null;
                                    RaisePropertyChanged("Credentials");
                                }

                                MessengerInstance.Send<CloseSettingsMessage>(new CloseSettingsMessage());
                            }
                        });
                }
                return _doLogin;
            }
        }

        public string SelectedCredential
        {
            get
            {
                return null;
            }
            set
            {
                if (value != null)
                {
                    AsyncInfo.Run(async (c) =>
                        {
                            var loggedInUser = await _userService.TryStoredLogin(value);
                            if (loggedInUser != null)
                            {
                                MessengerInstance.Send<CloseSettingsMessage>(new CloseSettingsMessage());
                            }
                            else
                            {
                                await _userService.RemoveStoredCredential(value);
                                _credentials = null;
                                RaisePropertyChanged("Credentials");
                            }
                        });
                }
            }
        }

        public class StoredCredentialCollection : ObservableCollection<string>, ISupportIncrementalLoading
        {
            public IUsersService UserService { get; set; }
            private bool _loaded;
            public bool HasMoreItems
            {
                //have a good url or are currently uninitialized
                get { return !_loaded; }
            }

            public async Task<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                _loaded = true;
                var storedCredentials = await UserService.StoredCredentials();

                foreach (var credential in storedCredentials)
                {
                    Add(credential.Username);
                }

                return new LoadMoreItemsResult { Count = (uint)storedCredentials.Count };
            }

            Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> ISupportIncrementalLoading.LoadMoreItemsAsync(uint count)
            {
                return AsyncInfo.Run((c) => LoadMoreItemsAsync(count));
            }
        }

        private StoredCredentialCollection _credentials;
        public StoredCredentialCollection Credentials
        {
            get
            {
                if (_credentials == null)
                    _credentials = new StoredCredentialCollection { UserService = _userService };

                return _credentials;
            }
        }
    }
}
