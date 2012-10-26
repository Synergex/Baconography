using GalaSoft.MvvmLight;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Things;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Data;
using System;
using Baconography.Services;
using Baconography.Messages;
using GalaSoft.MvvmLight.Command;
using Windows.ApplicationModel.DataTransfer;
using Baconography.RedditAPI.Actions;

namespace Baconography.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class CommentsViewModel : ViewModelBase
    {
        IUsersService _userService;
        IRedditActionQueue _actionQueue;
        INavigationService _nav;
        TypedThing<Link> _linkThing;
        /// <summary>
        /// Initializes a new instance of the CommentsViewModel class.
        /// </summary>
        public CommentsViewModel(IUsersService userService, IRedditActionQueue actionQueue, INavigationService nav)
        {
            _userService = userService;
            _actionQueue = actionQueue;
            _nav = nav;
            //Comments = new CommentViewModelCollection("/r/pics", "/r/pics/comments/117av1/i_sent_tom_hanks_a_1934_smith_corona_typewriter/", "t1_117av1", _userService, _actionQueue, _nav);
            
            MessengerInstance.Register<SelectCommentTree>(this, (msg) => LoadLink(msg.LinkThing, msg.RootComment));

            MessengerInstance.Register<ConnectionStatusMessage>(this, (connection) =>
            {
                if (IsOnline != connection.IsOnline)
                {
                    _isOnline = connection.IsOnline;
                    RaisePropertyChanged("IsOnline");
                }
            });
        }

        private void LoadLink(TypedThing<Link> link, TypedThing<Comment> rootComment)
        {
            _linkThing = link;
            Comments = new CommentViewModelCollection(Subreddit, _linkThing.Data.Permalink, _linkThing.Data.Name, _linkThing.Data.SubredditId, _userService, _actionQueue, _nav, link != null ? link.Data.Author : null);
            if (Comments.HasMoreItems)
            {
                //kick off the initial load of comments now that we know where we're going
                //((ISupportIncrementalLoading)Comments).LoadMoreItemsAsync(500);
            }
        }

        public CommentViewModelCollection Comments { get; private set; }

        private bool _isOnline;
        public bool IsOnline
        {
            get
            {
                return _isOnline;
            }
            set
            {
                _isOnline = value;
                RaisePropertyChanged("IsOnline");
            }
        }

        public string Title
        {
            get
            {
                return _linkThing.Data.Title;
            }
        }

        public string Url
        {
            get
            {
                return _linkThing.Data.Url;
            }
        }

        public string Author
        {
            get
            {
                return _linkThing.Data.Author;
            }
        }

        public string SelfText
        {
            get
            {
                return _linkThing.Data.Selftext;
            }
        }

        public bool IsSelf
        {
            get
            {
                return _linkThing.Data.IsSelf;
            }
        }

        public DateTime CreatedUTC
        {
            get
            {
                return _linkThing.Data.CreatedUTC;
            }
        }

        VotableViewModel _votable;
        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(new TypedThing<IVotable>(_linkThing), _actionQueue);
                return _votable;
            }
        }

        public string Subreddit
        {
            get
            {
                return _linkThing.Data.Subreddit;
            }
        }

        RelayCommand _saveLink;
        public RelayCommand SaveLink
        {
            get
            {
                if (_saveLink == null)
                {
                    _saveLink = new RelayCommand(() =>
                    {
                        var saveThing = new AddSavedThing { Id = _linkThing.Data.Name };
                        _actionQueue.AddAction(saveThing);
                    });
                }
                return _saveLink;
            }
        }

        RelayCommand _reportLink;
        public RelayCommand ReportLink
        {
            get
            {
                if (_reportLink == null)
                {
                    _reportLink = new RelayCommand(() =>
                    {
                        var reportThing = new AddReportOnThing { Id = _linkThing.Data.Name };
                        _actionQueue.AddAction(reportThing);
                    });
                }
                return _reportLink;
            }
        }

        RelayCommand _gotoReply;
        public RelayCommand GotoReply
        {
            get
            {
                if (_gotoReply == null)
                {
                    _gotoReply = new RelayCommand(() =>
                    {
                        ReplyData = new ReplyViewModel(_linkThing, _userService, _actionQueue, new RelayCommand(() => ReplyData = null),
                            (madeComment) => Comments.Add(new CommentViewModel(madeComment, _linkThing.Data.Name, _actionQueue, _nav, _userService, true, _linkThing.Data.Author)));
                    });
                }
                return _gotoReply;
            }
        }

        private ReplyViewModel _replyData;
        public ReplyViewModel ReplyData
        {
            get
            {
                return _replyData;
            }
            set
            {
                _replyData = value;
                RaisePropertyChanged("ReplyData");
            }
        }
    }
}