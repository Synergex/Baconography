using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class ComposePostViewModel : ViewModelBase
    {
        IBaconProvider _baconProvider;
        IUserService _userService;
        IRedditService _redditService;
        INavigationService _navigationService;
        IDynamicViewLocator _dynamicViewLocator;
        INotificationService _notificationService;
        MessageViewModel _replyMessage;

        public ComposePostViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _navigationService = baconProvider.GetService<INavigationService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();
            _notificationService = baconProvider.GetService<INotificationService>();
            _refreshUser = new RelayCommand(RefreshUserImpl);
            _submit = new RelayCommand(SubmitImpl);

            RefreshUserImpl();
        }

        public void SetEditorMode(TypedThing<Link> link)
        {
            Kind = link.Kind;
            Subreddit = link.Data.Subreddit;
            Title = link.Data.Title;
            Text = link.Data.Selftext;
            _name = link.Data.Name;
            Editing = true;
        }

        private bool _editing = false;
        public bool Editing
        {
            get
            {
                return _editing;
            }
            set
            {
                _editing = value;
                RaisePropertyChanged("Editing");
            }
        }

        private string _name;

        private string _kind;
        public string Kind
        {
            get
            {
                return _kind;
            }
            set
            {
                _kind = value;
                RaisePropertyChanged("Kind");
            }
        }

        private string _subreddit;
        public string Subreddit
        {
            get
            {
                return _subreddit;
            }
            set
            {
                _subreddit = value;
                RaisePropertyChanged("Subreddit");
            }
        }

        private string _title;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                RaisePropertyChanged("Title");
            }
        }

        private string _text;
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                RaisePropertyChanged("Text");
            }
        }

        private string _url;
        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
                RaisePropertyChanged("Url");
            }
        }

        private string _postingAs;
        public string PostingAs
        {
            get
            {
                return _postingAs;
            }
            set
            {
                _postingAs = value;
                RaisePropertyChanged("PostingAs");
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
                RaisePropertyChanged("CanSend");
            }
        }

        private bool _canSend;
        public bool CanSend
        {
            get
            {
                return IsLoggedIn
                    && (!String.IsNullOrWhiteSpace(Text) || !String.IsNullOrWhiteSpace(Url))
                    && !String.IsNullOrWhiteSpace(Title)
                    && !String.IsNullOrWhiteSpace(Subreddit);
            }
        }

        public RelayCommand Submit { get { return _submit; } }
        private RelayCommand _submit;
        private async void SubmitImpl()
        {
            if (Url == null)
                Url = "";
            if (Text == null)
                Text = "";

            try
            {
                MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = true });

                if (!Editing)
                {
                    await _redditService.AddPost(Kind, Url, Text, Subreddit, Title);
                }
                else
                {
                    await _redditService.EditPost(Text, _name);
                }

                Url = "";
                Text = "";
                Subreddit = "";
                Title = "";

                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _notificationService.CreateNotification("something bad happened while trying to submit your post: " + ex.ToString());
            }
            finally
            {
                MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            }
            
        }

        public RelayCommand RefreshUser { get { return _refreshUser; } }
        private RelayCommand _refreshUser;
        private void RefreshUserImpl()
        {
            var userServiceTask = _userService.GetUser();
            userServiceTask.Wait();

            if (string.IsNullOrWhiteSpace(userServiceTask.Result.Username))
            {
                IsLoggedIn = false;
                PostingAs = string.Empty;
            }
            else
            {
                PostingAs = userServiceTask.Result.Username;
                IsLoggedIn = true;
            }
        }
    }
}
