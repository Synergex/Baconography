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
    public class CommentsViewModel : ViewModelBase
    {
        IBaconProvider _baconProvider;
        IUserService _userService;
        IRedditService _redditService;
        INavigationService _navigationService;
        IDynamicViewLocator _dynamicViewLocator;
        TypedThing<Link> _linkThing;
        /// <summary>
        /// Initializes a new instance of the CommentsViewModel class.
        /// </summary>
        public CommentsViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _navigationService = baconProvider.GetService<INavigationService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();

            MessengerInstance.Register<SelectCommentTreeMessage>(this, OnComentTreeSelection);
            MessengerInstance.Register<ConnectionStatusMessage>(this, OnConnectionStatusChanged);

            _gotoLink = new RelayCommand(GotoLinkImpl);
            _gotoSubreddit = new RelayCommand(GotoSubredditImpl);
            _gotoUserDetails = new RelayCommand(GotoUserImpl);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (Comments != null)
            {
                Comments.Clear();
                Comments.Dispose();
            }
            Comments = null;
            _linkThing = null;
        }

        private void OnComentTreeSelection(SelectCommentTreeMessage msg)
        {
            LoadLink(msg.LinkThing, msg.RootComment);
        }

		private void OnConnectionStatusChanged(ConnectionStatusMessage connection)
        {
            if (IsOnline != connection.IsOnline)
            {
                _isOnline = connection.IsOnline;
                RaisePropertyChanged("IsOnline");
            }
        }

        private void LoadLink(TypedThing<Link> link, TypedThing<Comment> rootComment)
        {
            _linkThing = link;
            Comments = new CommentViewModelCollection(_baconProvider, _linkThing.Data.Permalink, _linkThing.Data.Subreddit, _linkThing.Data.SubredditId, _linkThing.Data.Name);
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

		private LinkViewModel _linkViewModel = null;
		public LinkViewModel Link
		{
			get
			{
				if (_linkViewModel == null)
				{
					_linkViewModel = new LinkViewModel(_linkThing, _baconProvider);
				}
				return _linkViewModel;
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

		public bool IsSelfPost
		{
			get
			{
				return _linkThing.Data.IsSelf;
			}
		}

		public bool HasThumbnail
		{
			get
			{
				return !string.IsNullOrWhiteSpace(Thumbnail) && Thumbnail != "self" && Thumbnail != "nsfw" && Thumbnail != "default";
			}
		}

		public string Thumbnail
		{
			get
			{
				return _linkThing.Data.Thumbnail;
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
                    _votable = new VotableViewModel(new TypedThing<IVotable>(_linkThing), _baconProvider);
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

        static RelayCommand<CommentsViewModel> _saveLink = new RelayCommand<CommentsViewModel>((vm) => vm.SaveLinkImpl());
        static RelayCommand<CommentsViewModel> _reportLink = new RelayCommand<CommentsViewModel>((vm) => vm.ReportLinkImpl());
        static RelayCommand<CommentsViewModel> _gotoReply = new RelayCommand<CommentsViewModel>((vm) => vm.GotoReplyImpl());
        RelayCommand _gotoLink;
        RelayCommand _gotoSubreddit;
        RelayCommand _gotoUserDetails;

        public RelayCommand<CommentsViewModel> SaveLink { get { return _saveLink; } }
        public RelayCommand<CommentsViewModel> ReportLink { get { return _reportLink; } }
        public RelayCommand<CommentsViewModel> GotoReply { get { return _gotoReply; } }
        public RelayCommand GotoLink { get { return _gotoLink; } }
        public RelayCommand GotoSubreddit { get { return _gotoSubreddit; } }
        public RelayCommand GotoUserDetails { get { return _gotoUserDetails; } }

        private void SaveLinkImpl()
        {
            //TODO: is this the right name?
            _redditService.AddSavedThing(_linkThing.Data.Name);
        }

        private void GotoLinkImpl()
        {
            UtilityCommandImpl.GotoLinkImpl(_linkThing.Data.Url);
        }

        private void GotoUserImpl()
        {
            UtilityCommandImpl.GotoUserDetails(_linkThing.Data.Author);
        }

        private async void GotoSubredditImpl()
        {
            _navigationService.Navigate(_dynamicViewLocator.RedditView, new SelectSubredditMessage { Subreddit = await _redditService.GetSubreddit(_linkThing.Data.Subreddit) });
        }

        private void ReportLinkImpl()
        {
            //TODO: is this the right name?
            _redditService.AddReportOnThing(_linkThing.Data.Name);
        }

        private void GotoReplyImpl()
        {
            Action<Thing> uiResponse = (madeComment) => Comments.Add(new CommentViewModel(_baconProvider, madeComment, _linkThing.Data.Name, false));
            ReplyData = new ReplyViewModel(_baconProvider, _linkThing, new RelayCommand(() => ReplyData = null), uiResponse);
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
