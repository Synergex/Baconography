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
    public class CommentViewModel : ViewModelBase
    {
        TypedThing<Comment> _comment;
        IRedditService _redditService;
        INavigationService _navigationService;
        IUserService _userService;
        IDynamicViewLocator _dynamicViewLocator;
        IBaconProvider _baconProvider;
        private ReplyViewModel _replyData;
        CommentViewModelCollection _replies;
        private bool _isMinimized;
        string _linkId;
        string _opName;

        public CommentViewModel(IBaconProvider baconProvider, Thing comment, string linkId, bool oddNesting, string opName)
        {
            _comment = new TypedThing<Comment>(comment);
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();
            _linkId = linkId;
            OddNesting = oddNesting;
            _opName = opName;
        }

        public bool OddNesting { get; private set; }

        VotableViewModel _votable;
        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(new TypedThing<IVotable>(_comment), _baconProvider);
                return _votable;
            }
        }

        public CommentViewModelCollection Replies
        {
            get
            {
                if (_replies == null)
                {
                    if (_comment.Data.Replies != null)
                    {
                        _replies = new CommentViewModelCollection(_baconProvider, _comment.Data.Subreddit, _comment.Data.Name, 
                            _linkId, _comment.Data.SubredditId,_comment.Data.Replies.Data.Children, OddNesting, _opName);
                    }
                    else
                    {
                        _replies = new CommentViewModelCollection(_baconProvider, _comment.Data.Subreddit, _comment.Data.Name, _linkId,
                            _comment.Data.SubredditId, new List<Thing> { }, OddNesting, _opName);
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

        public AuthorFlairKind PosterFlair
        {
            get
            {
                if (_opName == _comment.Data.Author)
                    return AuthorFlairKind.OriginalPoster;

                else
                    return AuthorFlairKind.None;
            }
        }

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


        public RelayCommand<CommentViewModel> MinimizeCommand { get { return _minimizeCommand; } }
        public RelayCommand<CommentViewModel> MaximizeCommand { get { return _maximizeCommand; } }
        public RelayCommand<CommentViewModel> GotoContext { get { return _gotoContext; } }
        public RelayCommand<CommentViewModel> GotoFullLink { get { return _gotoFullLink; } }
        public RelayCommand<CommentViewModel> Report { get { return _report; } }
        public RelayCommand<CommentViewModel> Save { get { return _save; } }
        public RelayCommand<CommentViewModel> GotoReply { get { return _gotoReply; } }

        static RelayCommand<CommentViewModel> _gotoReply = new RelayCommand<CommentViewModel>((vm) => vm.GotoReplyImpl());
        static RelayCommand<CommentViewModel> _save = new RelayCommand<CommentViewModel>((vm) => vm.SaveImpl());
        static RelayCommand<CommentViewModel> _report = new RelayCommand<CommentViewModel>((vm) => vm.ReportImpl());
        static RelayCommand<CommentViewModel> _gotoFullLink = new RelayCommand<CommentViewModel>((vm) => vm.GotoFullLinkImpl());
        static RelayCommand<CommentViewModel> _gotoContext = new RelayCommand<CommentViewModel>((vm) => vm.GotoContextImpl());

        static RelayCommand<CommentViewModel> _maximizeCommand = new RelayCommand<CommentViewModel>((vm) => vm.IsMinimized = false);
        static RelayCommand<CommentViewModel> _minimizeCommand = new RelayCommand<CommentViewModel>((vm) => vm.IsMinimized = true );


        private void GotoContextImpl()
        {
            var commentTree = new SelectCommentTreeMessage { RootComment = _comment, Context = 3 };
            _navigationService.Navigate(_dynamicViewLocator.CommentsView, commentTree);
        }

        private async void GotoFullLinkImpl()
        {
            MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var linkThing = await _redditService.GetThingById(_comment.Data.LinkId);
            var commentTree = new SelectCommentTreeMessage { RootComment = _comment, Context = 3, LinkThing = new TypedThing<Link>(linkThing) };
            MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            _navigationService.Navigate(_dynamicViewLocator.CommentsView, commentTree);
        }

        private void ReportImpl()
        {
            _redditService.AddReportOnThing(_comment.Data.Name);
        }

        private void SaveImpl()
        {
            _redditService.AddSavedThing(_comment.Data.Name);
        }

        private void GotoReplyImpl()
        {
            ReplyData = new ReplyViewModel(_baconProvider, _comment, new RelayCommand(() => ReplyData = null),
                            (madeComment) => _replies.Add(new CommentViewModel(_baconProvider, madeComment, _linkId, !OddNesting, _opName)));
        }

    }
}
