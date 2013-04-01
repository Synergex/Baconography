using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class LinkViewModel : ViewModelBase
    {
        TypedThing<Link> _linkThing;
        IRedditService _redditService;
        INavigationService _navigationService;
        IImagesService _imagesService;
        IDynamicViewLocator _dynamicViewLocator;
        IBaconProvider _baconProvider;
        bool _isPreviewShown;
		bool _isExtendedOptionsShown;

        public LinkViewModel(Thing linkThing, IBaconProvider baconProvider)
        {
            _linkThing = new TypedThing<Link>(linkThing);
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _imagesService = _baconProvider.GetService<IImagesService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();
            _isPreviewShown = false;
			_isExtendedOptionsShown = false;
            ShowPreview = new RelayCommand(() => IsPreviewShown = !IsPreviewShown);
			ShowExtendedOptions = new RelayCommand(() => IsExtendedOptionsShown = !IsExtendedOptionsShown);
        }

        VotableViewModel _votable;
        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(_linkThing, _baconProvider);
                return _votable;
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

        public DateTime CreatedUTC
        {
            get
            {
                return _linkThing.Data.CreatedUTC;
            }
        }

        //this should show only moderator info
        public AuthorFlairKind AuthorFlair
        {
            get
            {
                return AuthorFlairKind.None;
            }
        }

        public string Author
        {
            get
            {
                return _linkThing.Data.Author;
            }
        }

        public string Subreddit
        {
            get
            {
                return _linkThing.Data.Subreddit;
            }
        }

        public string Title
        {
            get
            {
                return _linkThing.Data.Title.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim();
            }
        }

        public int CommentCount
        {
            get
            {
                return _linkThing.Data.CommentCount;
            }
        }

        public bool IsSelfPost
        {
            get
            {
                return _linkThing.Data.IsSelf;
            }
        }

        public string Url
        {
            get
            {
                return _linkThing.Data.Url;
            }
        }

		string _domain = null;
		public string Domain
		{
			get
			{
				if (_domain == null)
				{
					_domain = (new Uri(Url)).Host.TrimStart(new char[] { 'w', '.' });
					if (_domain == "reddit.com")
						_domain = "self." + Subreddit.ToLower();
				}
				return _domain;
			}
		}

        public string Id
        {
            get
            {
                return _linkThing.Data.Id;
            }
        }

        public bool HasPreview
        {
            get
            {
                return _imagesService.MightHaveImagesFromUrl(Url);
            }
        }

        public bool IsPreviewShown
        {
            get
            {
                return _isPreviewShown;
            }
            set
            {
                _isPreviewShown = value;
                RaisePropertyChanged("IsPreviewShown");
                RaisePropertyChanged("PreviewPack");
            }
        }

		public bool IsExtendedOptionsShown
		{
			get
			{
				return _isExtendedOptionsShown;
			}
			set
			{
				_isExtendedOptionsShown = value;
				RaisePropertyChanged("IsExtendedOptionsShown");
			}
		}

        public Tuple<bool, string> PreviewPack
        {
            get
            {
                return Tuple.Create(IsPreviewShown, Url);
            }
        }

        public RelayCommand<LinkViewModel> NavigateToComments { get { return _navigateToComments; } }
        public RelayCommand<LinkViewModel> GotoLink { get { return _gotoLink; } }
		public RelayCommand<LinkViewModel> GotoSubreddit { get { return _gotoSubreddit; } }
		public RelayCommand<LinkViewModel> GotoUserDetails { get { return _gotoUserDetails; } }

        static RelayCommand<LinkViewModel> _navigateToComments = new RelayCommand<LinkViewModel>(NavigateToCommentsImpl);
        static RelayCommand<LinkViewModel> _gotoLink = new RelayCommand<LinkViewModel>(GotoLinkImpl);
		static RelayCommand<LinkViewModel> _gotoSubreddit = new RelayCommand<LinkViewModel>(GotoSubredditStatic);
		static RelayCommand<LinkViewModel> _gotoUserDetails = new RelayCommand<LinkViewModel>(GotoUserStatic);

        public RelayCommand ShowPreview { get; set; }
		public RelayCommand ShowExtendedOptions { get; set; }

		private static void GotoSubredditStatic(LinkViewModel vm)
		{
			vm.GotoSubredditImpl();
		}

		private static void GotoUserStatic(LinkViewModel vm)
		{
			vm.GotoUserImpl();
		}

		private async void GotoSubredditImpl()
        {
            _navigationService.Navigate(_dynamicViewLocator.RedditView, new SelectSubredditMessage { Subreddit = await _redditService.GetSubreddit(_linkThing.Data.Subreddit) });
        }

		private void GotoUserImpl()
        {
            UtilityCommandImpl.GotoUserDetails(_linkThing.Data.Author);
        }

		public void GotoComments()
		{
			NavigateToCommentsImpl(this);
		}

        private static void NavigateToCommentsImpl(LinkViewModel vm)
        {
            vm._navigationService.Navigate(vm._dynamicViewLocator.CommentsView, new SelectCommentTreeMessage { LinkThing = vm._linkThing });
        }

        private static void GotoLinkImpl(LinkViewModel vm)
        {
            UtilityCommandImpl.GotoLinkImpl(vm.Url);
        }
    }
}
