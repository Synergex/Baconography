using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Actions;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using Baconography.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Baconography.ViewModel
{
    public class CommentViewModel : ViewModelBase
    {
        TypedThing<Comment> _comment;
        IRedditActionQueue _actionQueue;
        INavigationService _nav;
        IUsersService _userService;
        string _linkId;
        string _opName;

        public CommentViewModel(Thing comment, string linkId, IRedditActionQueue actionQueue, INavigationService nav, IUsersService userService, bool oddNesting, string opName)
        {
            _comment = new TypedThing<Comment>(comment);
            _actionQueue = actionQueue;
            _nav = nav;
            _userService = userService;
            _linkId = linkId;
            OddNesting = oddNesting;
            _opName = opName;
        }

        public bool OddNesting {get; private set;}

        VotableViewModel _votable;
        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(new TypedThing<IVotable>(_comment), _actionQueue);
                return _votable;
            }
        }

        CommentViewModelCollection _replies;
        public CommentViewModelCollection Replies
        {
            get
            {
                if(_replies == null)
                {
                    if (_comment.Data.Replies != null)
                    {
                        _replies = new CommentViewModelCollection(_comment.Data.Subreddit, _comment.Data.Name, _linkId, _comment.Data.SubredditId,
                            _userService, _actionQueue, _nav, _comment.Data.Replies.Data.Children, OddNesting, _opName);
                    }
                    else
                    {
                        _replies = new CommentViewModelCollection(_comment.Data.Subreddit, _comment.Data.Name, _linkId, _comment.Data.SubredditId,
                            _userService, _actionQueue, _nav, new List<Thing> { }, OddNesting, _opName);
                    }
                }
                return _replies;
            }
        }

        public DateTime CreatedUTC
        {
            get
            {
                return _comment.Data.CreatedUTC;
            }
        }

        public string Body
        {
            get
            {
                return _comment.Data.Body;
            }
        }

        public string PosterName
        {
            get
            {
                return _comment.Data.Author;
            }
        }

        public Brush PosterFlair
        {
            get
            {
                if (_opName == _comment.Data.Author)
                    return new SolidColorBrush(Colors.DarkBlue);
                
                else
                    return new SolidColorBrush(Colors.Transparent);
            }
        }

        private bool _isMinimized;
        public bool IsMinimized
        {
            get
            {
                return _isMinimized;
            }
            set
            {
                _isMinimized = value;
                RaisePropertyChanged("IsMinimized");
            }
        }

        RelayCommand _minimizeCommand;
        public RelayCommand MinimizeCommand
        {
            get
            {
                if (_minimizeCommand == null)
                {
                    _minimizeCommand = new RelayCommand(() =>
                    {
                        IsMinimized = true;
                    });
                }
                return _minimizeCommand;
            }
        }
        

        RelayCommand _maximizeCommand;
        public RelayCommand MaximizeCommand
        {
            get
            {
                if (_maximizeCommand == null)
                {
                    _maximizeCommand = new RelayCommand(() => 
                    {
                        IsMinimized = false;
                    });
                }
                return _maximizeCommand;
            }
        }

        RelayCommand _gotoContext;
        public RelayCommand GotoContext
        {
            get
            {
                if (_gotoContext == null)
                {
                    _gotoContext = new RelayCommand(() => 
                    { 
                        var commentTree = new SelectCommentTree { RootComment = _comment, Context = 3 };
                        _nav.Navigate<CommentView>(commentTree);
                    });
                }
                return _gotoContext;
            }
        }

        RelayCommand _gotoFullLink;
        public RelayCommand GotoFullLink
        {
            get
            {
                if (_gotoFullLink == null)
                {
                    _gotoFullLink = new RelayCommand(async () =>
                    {
                        MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                        var thingGetter = new GetThingById { Id = _comment.Data.LinkId };
                        var commentTree = new SelectCommentTree { RootComment = _comment, Context = 3, LinkThing = new TypedThing<Link>(await thingGetter.Run()) };
                        MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                        
                        _nav.Navigate<CommentView>(commentTree);
                    });
                }
                return _gotoFullLink;
            }
        }

        RelayCommand _report;
        public RelayCommand Report
        {
            get
            {
                if (_report == null)
                {
                    _report = new RelayCommand(() =>
                    {
                        var reportThing = new AddReportOnThing { Id = _comment.Data.Name };
                        _actionQueue.AddAction(reportThing);
                    });
                }
                return _report;
            }
        }


        RelayCommand _save;
        public RelayCommand Save
        {
            get
            {
                if (_save == null)
                {
                    _save = new RelayCommand(() =>
                    {
                        var saveThing = new AddSavedThing { Id = _comment.Data.Name };
                        _actionQueue.AddAction(saveThing);
                    });
                }
                return _save;
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
                        ReplyData = new ReplyViewModel(_comment, _userService, _actionQueue, new RelayCommand(() => ReplyData = null), 
                            (madeComment) => _replies.Add(new CommentViewModel(madeComment, _linkId, _actionQueue, _nav, _userService, !OddNesting, _opName)));
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
