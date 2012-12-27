﻿using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        ReplyViewModel _replyData;
        ObservableCollection<ViewModelBase> _replies;
        private bool _isMinimized;
        private bool _isExtended;
        string _linkId;

        public CommentViewModel(IBaconProvider baconProvider, Thing comment, string linkId, bool oddNesting)
        {
            _isMinimized = false;
            _comment = new TypedThing<Comment>(comment);
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();
            _linkId = linkId;
            OddNesting = oddNesting;
            AuthorFlair = _redditService.GetUsernameModifiers(_comment.Data.Author, _linkId, _comment.Data.Subreddit);
            _showExtendedView = new RelayCommand(ShowExtendedViewImpl);
            _gotoReply = new RelayCommand(GotoReplyImpl);
            _save = new RelayCommand(SaveImpl);
            _report = new RelayCommand(ReportImpl);
            _gotoFullLink = new RelayCommand(GotoFullLinkImpl);
            _gotoContext = new RelayCommand(GotoContextImpl);
            _gotoUserDetails = new RelayCommand(GotoUserDetailsImpl);
            _minimizeCommand = new RelayCommand(() => IsMinimized = !IsMinimized);
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

        AuthorFlairKind AuthorFlair { get; set; }

        public ObservableCollection<ViewModelBase> Replies
        {
            get
            {
                return _replies;
            }
            set
            {
                _replies = value;
                RaisePropertyChanged("Replies");
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

        public bool IsExtended
        {
            get
            {
                return _isExtended;
            }
            set
            {
                _isExtended = value;
                RaisePropertyChanged("IsExtended");
                RaisePropertyChanged("ExtendedData");
            }
        }

        public Tuple<bool, CommentViewModel> ExtendedData
        {
            get
            {
                return Tuple.Create(IsExtended, this);
            }
        }

        public AuthorFlairKind PosterFlair
        {
            get
            {
                return _redditService.GetUsernameModifiers(PosterName, _linkId, _comment.Data.SubredditId);
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


        public RelayCommand MinimizeCommand { get { return _minimizeCommand; } }
        public RelayCommand ShowExtendedView { get { return _showExtendedView; } }
        public RelayCommand GotoContext { get { return _gotoContext; } }
        public RelayCommand GotoFullLink { get { return _gotoFullLink; } }
        public RelayCommand Report { get { return _report; } }
        public RelayCommand Save { get { return _save; } }
        public RelayCommand GotoReply { get { return _gotoReply; } }
        public RelayCommand GotoUserDetails { get { return _gotoUserDetails; } }

        RelayCommand _showExtendedView;

        RelayCommand _gotoReply;
        RelayCommand _save;
        RelayCommand _report;
        RelayCommand _gotoFullLink;
        RelayCommand _gotoContext;
        RelayCommand _minimizeCommand;
        RelayCommand _gotoUserDetails;

        private void ShowExtendedViewImpl()
        {
            IsExtended = !IsExtended;
        }

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

        private async void GotoUserDetailsImpl()
        {
            var getAccount =  await _redditService.GetAccountInfo(_comment.Data.Author);
            var accountMessage = new SelectUserAccountMessage { Account = getAccount};
            _navigationService.Navigate(_dynamicViewLocator.AboutUserView, accountMessage);
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
            if (ReplyData != null)
                ReplyData = null;
            else
                ReplyData = new ReplyViewModel(_baconProvider, _comment, new RelayCommand(() => ReplyData = null),
                            (madeComment) => _replies.Add(new CommentViewModel(_baconProvider, madeComment, _linkId, !OddNesting)));
        }

    }
}
