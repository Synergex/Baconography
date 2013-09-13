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
    public class ComposeViewModel : ViewModelBase
    {
        IBaconProvider _baconProvider;
        IUserService _userService;
        IRedditService _redditService;
        INavigationService _navigationService;
        IDynamicViewLocator _dynamicViewLocator;
        INotificationService _notificationService;
        MessageViewModel _replyMessage;

        public ComposeViewModel(IBaconProvider baconProvider, MessageViewModel replyMessage = null)
        {
            _baconProvider = baconProvider;
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _navigationService = baconProvider.GetService<INavigationService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();
            _notificationService = baconProvider.GetService<INotificationService>();
            _refreshUser = new RelayCommand(RefreshUserImpl);
            _send = new RelayCommand(SendImpl);

            RefreshUserImpl();

            if (replyMessage != null)
            {
                IsReply = true;
                _replyMessage = replyMessage;
                _subject = _replyMessage.Subject;
                _recipient = _replyMessage.Author;
            }
        }

        public bool IsReply
        {
            get;
            set;
        }

        private string _recipient;
        public string Recipient
        {
            get
            {
                return _recipient;
            }
            set
            {
                _recipient = value;
                RaisePropertyChanged("Recipient");
                RaisePropertyChanged("CanSend");
            }
        }

        private string _subject;
        public string Subject
        {
            get
            {
                return _subject;
            }
            set
            {
                _subject = value;
                RaisePropertyChanged("Subject");
                RaisePropertyChanged("CanSend");
            }
        }

        private string _message;
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                RaisePropertyChanged("Message");
                RaisePropertyChanged("CanSend");
            }
        }

        private string _commentingAs;
        public string CommentingAs
        {
            get
            {
                return _commentingAs;
            }
            set
            {
                _commentingAs = value;
                RaisePropertyChanged("CommentingAs");
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
                    && !String.IsNullOrWhiteSpace(Subject)
                    && !String.IsNullOrWhiteSpace(Message)
                    && !String.IsNullOrWhiteSpace(Recipient);
            }
        }

        public RelayCommand Send { get { return _send; } }
        private RelayCommand _send;
        private async void SendImpl()
        {
            try
            {
                MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = true });

                await _redditService.AddMessage(_recipient, _subject, _message);

                this.Message = "";
                this.Recipient = "";
                this.Subject = "";

                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _notificationService.CreateNotification("something bad happened while trying to submit your PM: " + ex.ToString());
            }
            finally
            {
                MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            }

            // TODO: Content for SENT view
            /*
            if (IsReply && (_replyMessage.IsPostReply || _replyMessage.IsCommentReply))
            {
                var theReply = new Thing
                {
                    Kind = "t4.5",
                    Data = new CommentMessage
                    {
                        Author = CommentingAs,
                        Body = _message,
                        Created = DateTime.Now,
                        CreatedUTC = DateTime.UtcNow,
                        Destination = _recipient,
                        LinkTitle = _subject,
                        ParentId = _replyMessage.Id,
                        Subject = _replyMessage.IsPostReply ? "post reply" : "comment reply",
                        Subreddit = _replyMessage.Subreddit,
                        WasComment = true,
                    }
                };
            }
            var theComment = new Thing
            {
                Kind = "t4",
                Data = new Message
                {
                    Author = string.IsNullOrWhiteSpace(CommentingAs) ? "self" : CommentingAs,
                    Body = _message,
                    Created = DateTime.Now,
                    CreatedUTC = DateTime.UtcNow,
                    Destination = _recipient,
                    Subject = _subject,
                }
            };
            */
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
                CommentingAs = string.Empty;
            }
            else
            {
                CommentingAs = userServiceTask.Result.Username;
                IsLoggedIn = true;
            }
        }
    }
}
