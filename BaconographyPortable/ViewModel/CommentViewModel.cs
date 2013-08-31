using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
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
        IMarkdownProcessor _markdownProcessor;
        ReplyViewModel _replyData;
        List<ViewModelBase> _replies;
        private bool _isMinimized;
        private bool _isExtended;
        string _linkId;

        public CommentViewModel(IBaconProvider baconProvider, Thing comment, string linkId, bool oddNesting, int depth = 0)
        {
            _isMinimized = false;
            _comment = new TypedThing<Comment>(comment);
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();
            _markdownProcessor = _baconProvider.GetService<IMarkdownProcessor>();
            _linkId = linkId;
            OddNesting = oddNesting;
			Depth = depth;
            AuthorFlair = _redditService.GetUsernameModifiers(_comment.Data.Author, _linkId, _comment.Data.Subreddit);
            _showExtendedView = new RelayCommand(ShowExtendedViewImpl);
            _gotoReply = new RelayCommand(GotoReplyImpl);
            _save = new RelayCommand(SaveImpl);
            _report = new RelayCommand(ReportImpl);
            _gotoFullLink = new RelayCommand(GotoFullLinkImpl);
            _gotoContext = new RelayCommand(GotoContextImpl);
            _gotoUserDetails = new RelayCommand(GotoUserDetailsImpl);
            _gotoEdit = new RelayCommand(GotoEditImpl);
            _minimizeCommand = new RelayCommand(() => IsMinimized = !IsMinimized);
            Body = _comment.Data.Body;
        }

        public bool OddNesting { get; private set; }

        VotableViewModel _votable;
        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(new TypedThing<IVotable>(_comment), _baconProvider, () => RaisePropertyChanged("Votable"));
                return _votable;
            }
        }

		public int Depth { get; set; }

        AuthorFlairKind AuthorFlair { get; set; }

        public List<ViewModelBase> Replies
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

        private object _body;
        public object Body 
        {
            get
            {
                return _body;
            }
            set
            {
                if (value is string)
                    _body = _markdownProcessor.Process(value as string);
                else
                    _body = value;

                RaisePropertyChanged("Body");
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
				this.Touch();
            }
        }

		// Cause UI to re-evaluate visibility without changing values
		public void Touch()
		{
			if (Replies != null)
			{
				for (int i = 0; i < Replies.Count; i++)
				{
					var comment = Replies[i] as CommentViewModel;
					var more = Replies[i] as MoreViewModel;
					if (comment != null) comment.Touch();
					if (more != null) more.Touch();
				}
			}
			RaisePropertyChanged("IsVisible");
		}

		public CommentViewModel Parent
		{
			get;
			set;
		}

		public bool IsVisible
		{
			get
			{
				if (Parent != null)
				{
					return Parent.IsVisible ? !Parent.IsMinimized : false;
				}
				return true;
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

        public bool IsEditable
        {
            get
            {
                //this looks really bad but it shouldnt actually end up being an issue in practice because we 
                //shouldnt be in a state where we can be looking at a comment but be waiting on GetUser to return
                var userTask = _userService.GetUser();
                return userTask.IsCompleted && userTask.Result != null && userTask.Result.Username == PosterName;
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
        public RelayCommand GotoEdit { get { return _gotoEdit; } }
        public RelayCommand GotoUserDetails { get { return _gotoUserDetails; } }

        RelayCommand _showExtendedView;

        RelayCommand _gotoReply;
        RelayCommand _gotoEdit;
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

        private async void GotoContextImpl()
        {
            try
            {
                if (_comment.Data.ParentId == null)
                    return;

                MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                var linkThing = new TypedThing<Link>(await _redditService.GetThingById(_comment.Data.LinkId));
                var parentThing = await _redditService.GetLinkByUrl("http://www.reddit.com/" + linkThing.Data.Permalink + _comment.Data.ParentId.Substring(3));
                var commentTree = new SelectCommentTreeMessage { LinkThing = new TypedThing<Link>(parentThing) };
                MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                _navigationService.Navigate(_dynamicViewLocator.CommentsView, commentTree);
            }
            catch (Exception ex)
            {
                _baconProvider.GetService<INotificationService>().CreateErrorNotification(ex);
            }
        }

        private async void GotoFullLinkImpl()
        {
            MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            var linkThing = await _redditService.GetThingById(_comment.Data.LinkId);
            var commentTree = new SelectCommentTreeMessage { Context = 3, LinkThing = new TypedThing<Link>(linkThing) };
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
                            (madeComment) => _replies.Add(new CommentViewModel(_baconProvider, madeComment, _linkId, !OddNesting, Depth + 1)));
        }

        private void GotoEditImpl()
        {
            if (ReplyData != null)
                ReplyData = null;
            else
                ReplyData = new ReplyViewModel(_baconProvider, _comment, new RelayCommand(() => ReplyData = null),
                            (madeComment) => Body = ((Comment)madeComment.Data).Body, true);
        }

    }
}
