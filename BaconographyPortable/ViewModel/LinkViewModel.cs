using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class LinkViewModel : ViewModelBase, IMergableThing
    {
        TypedThing<Link> _linkThing;
        IRedditService _redditService;
        INavigationService _navigationService;
        IImagesService _imagesService;
        IDynamicViewLocator _dynamicViewLocator;
        IBaconProvider _baconProvider;
        ISettingsService _settingsService;
        bool _isPreviewShown;
		bool _isExtendedOptionsShown;
        bool _loading;
        bool _registeredLongNav;

        public LinkViewModel(Thing linkThing, IBaconProvider baconProvider, bool? wasStreamed = null)
        {
            _linkThing = new TypedThing<Link>(linkThing);
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _imagesService = _baconProvider.GetService<IImagesService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();
            _settingsService = _baconProvider.GetService<ISettingsService>();
            _isPreviewShown = false;
			_isExtendedOptionsShown = false;
            _loading = false;
            _registeredLongNav = false;
            ShowPreview = new RelayCommand(() => IsPreviewShown = !IsPreviewShown);
			ShowExtendedOptions = new RelayCommand(() => IsExtendedOptionsShown = !IsExtendedOptionsShown);
            WasStreamed = wasStreamed ?? false;

            ContentIsFocused = !WasStreamed;

            if (Url != null)
            {
                if (_imagesService.MightHaveImagesFromUrl(Url) && !Url.EndsWith(".jpg") && !Url.EndsWith(".gif") && !Url.EndsWith(".png"))
                {
                    MessengerInstance.Register<LongNavigationMessage>(this, OnLongNav);
                    _registeredLongNav = true;
                }
            }
        }

        public TypedThing<Link> LinkThing { get { return _linkThing; } }

        private void OnLongNav(LongNavigationMessage msg)
        {
            if (msg.TargetUrl == Url)
            {
                Loading = !msg.Finished;
            }
        }

        public override void Cleanup()
        {
            if (_registeredLongNav)
                MessengerInstance.Unregister<LongNavigationMessage>(this);
            base.Cleanup();
        }

        public bool Loading
        {
            get
            {
                return _loading;
            }
            set
            {
                _loading = value;
                RaisePropertyChanged("Loading");
            }
        }

        VotableViewModel _votable;
        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(_linkThing, _baconProvider, () => RaisePropertyChanged("Votable"));
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
					if (_domain == "reddit.com" && Url.ToLower().Contains(Subreddit.ToLower()))
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
        bool _hasBeenExtended = false;
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
                if (_isExtendedOptionsShown && !_hasBeenExtended)
                {
                    _hasBeenExtended = true;
                    RaisePropertyChanged("ExtendedData");
                }
			}
		}

        public Tuple<bool, LinkViewModel> ExtendedData
        {
            get
            {
                return Tuple.Create(_hasBeenExtended, this);
            }
        }

        public WeakReference ExtendedView { get; set; }

        public bool FromMultiReddit { get; set; }

        public Tuple<bool, string> PreviewPack
        {
            get
            {
                return Tuple.Create(IsPreviewShown, Url);
            }
        }

        private bool _contentIsFocused = false;
        public bool ContentIsFocused
        {
            get
            {
                return _contentIsFocused;
            }
            set
            {
                if (_contentIsFocused != value)
                {
                    _contentIsFocused = value;
                    RaisePropertyChanged("ContentIsChanged");
                }
            }
        }

        public bool WasStreamed { get; set; }

        object _selfText;
        public object SelfText
        {
            get
            {
                if (_selfText == null)
                {
                    _selfText = _baconProvider.GetService<IMarkdownProcessor>().Process(_linkThing.Data.Selftext);
                }
                return _selfText;
            }
        }

        LinkViewModel _parentLink;
        public LinkViewModel ParentLink
        {
            get
            {
                if (_parentLink == null)
                {
                    if (string.IsNullOrWhiteSpace(this.LinkThing.Data.Id))
                        return null;

                    var viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
                    var firstRedditViewModel = viewModelContextService.ContextStack.FirstOrDefault(context => context is RedditViewModel) as RedditViewModel;
                    if (firstRedditViewModel != null)
                    {
                        for (int i = 0; i < firstRedditViewModel.Links.Count; i++)
                        {
                            var linkViewModel = firstRedditViewModel.Links[i] as LinkViewModel;
                            if (linkViewModel != null)
                            {
                                if (linkViewModel.LinkThing.Data.Id == this.LinkThing.Data.Id)
                                {
                                    _parentLink = linkViewModel;
                                    break;
                                }
                            }
                        }
                    }
                }

                return _parentLink;
            }
        }

        public bool HasContext
        {
            get
            {
                return ParentLink != null;
            }
        }

        public RelayCommand<LinkViewModel> NavigateToComments { get { return _navigateToComments; } }
        public RelayCommand<LinkViewModel> GotoLink { get { return _gotoLink; } }
		public RelayCommand<LinkViewModel> GotoSubreddit { get { return _gotoSubreddit; } }
		public RelayCommand<LinkViewModel> GotoUserDetails { get { return _gotoUserDetails; } }
        public RelayCommand GotoUser { get { return new RelayCommand(() => GotoUserStatic(this)); } }

        static RelayCommand<LinkViewModel> _navigateToComments = new RelayCommand<LinkViewModel>(NavigateToCommentsImpl);
        static RelayCommand<LinkViewModel> _gotoLink = new RelayCommand<LinkViewModel>(GotoLinkImpl);
		static RelayCommand<LinkViewModel> _gotoSubreddit = new RelayCommand<LinkViewModel>(GotoSubredditStatic);
		static RelayCommand<LinkViewModel> _gotoUserDetails = new RelayCommand<LinkViewModel>(GotoUserStatic);

        public RelayCommand ShowPreview { get; set; }
		public RelayCommand ShowExtendedOptions { get; set; }

		private static void GotoSubredditStatic(LinkViewModel vm)
		{
            if (vm.IsExtendedOptionsShown)
                vm.IsExtendedOptionsShown = false;
			vm.GotoSubredditImpl();
		}

		private static void GotoUserStatic(LinkViewModel vm)
		{
            if (vm.IsExtendedOptionsShown)
                vm.IsExtendedOptionsShown = false;
			vm.GotoUserImpl();
		}

		private async void GotoSubredditImpl()
        {
            if (IsExtendedOptionsShown)
                IsExtendedOptionsShown = false;
            Messenger.Default.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = await _redditService.GetSubreddit(_linkThing.Data.Subreddit) });
        }

		private void GotoUserImpl()
        {
            if (IsExtendedOptionsShown)
                IsExtendedOptionsShown = false;

            UtilityCommandImpl.GotoUserDetails(_linkThing.Data.Author);
        }

		public void GotoComments()
		{
            if (_settingsService.TapForComments)
            {
			    NavigateToCommentsImpl(this);
                if (IsExtendedOptionsShown)
                    IsExtendedOptionsShown = false;
            }
            else
            {
                IsExtendedOptionsShown = !IsExtendedOptionsShown;
            }
		}

        private static void NavigateToCommentsImpl(LinkViewModel vm)
        {
            if (vm.IsExtendedOptionsShown)
                vm.IsExtendedOptionsShown = false;

            if (vm == null || vm._linkThing == null || vm._linkThing.Data == null || string.IsNullOrWhiteSpace(vm._linkThing.Data.Url))
                vm._baconProvider.GetService<INotificationService>().CreateNotification("Invalid link data, please PM /u/hippiehunter with details");
            else
                vm._navigationService.Navigate(vm._dynamicViewLocator.CommentsView, new SelectCommentTreeMessage { LinkThing = vm._linkThing });
        }

        private static void GotoLinkImpl(LinkViewModel vm)
        {
            if (vm.IsExtendedOptionsShown)
                vm.IsExtendedOptionsShown = false;

            if (vm.IsSelfPost && !vm._settingsService.OnlyFlipViewImages)
            {
                vm._navigationService.Navigate(vm._dynamicViewLocator.SelfPostView, Tuple.Create(vm.LinkThing, false));
            }
            else
            {
                UtilityCommandImpl.GotoLinkImpl(vm.Url, vm._linkThing);
            }
            vm.RaisePropertyChanged("Url");
        }

        private static async void UpdateUsageStatistics(LinkViewModel vm, bool isLink)
        {
            if (vm._linkThing != null)
            {
                var offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();

                await offlineService.IncrementDomainStatistic(vm._linkThing.Data.Domain, isLink);
                await offlineService.IncrementSubredditStatistic(vm._linkThing.Data.SubredditId, isLink);
            }
        }

        public bool MaybeMerge(ViewModelBase thing)
        {
            if (thing is LinkViewModel && ((LinkViewModel)thing).Id == Id)
            {
                _linkThing = ((LinkViewModel)thing).LinkThing;
                Votable.MergeVotable(_linkThing);
                RaisePropertyChanged("CommentCount");
                RaisePropertyChanged("CreatedUTC");
                return true;
            }
            else
                return false;
        }
    }
}
