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

        public LinkViewModel(Thing linkThing, IBaconProvider baconProvider)
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


            if (_imagesService.MightHaveImagesFromUrl(Url) && !Url.EndsWith(".jpg") && !Url.EndsWith(".gif") && !Url.EndsWith(".png"))
            {
                MessengerInstance.Register<LongNavigationMessage>(this, OnLongNav);
                _registeredLongNav = true;
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
                RaisePropertyChanged("ExtendedData");
			}
		}

        public Tuple<bool, LinkViewModel> ExtendedData
        {
            get
            {
                return Tuple.Create(IsExtendedOptionsShown, this);
            }
        }

        public bool FromMultiReddit { get; set; }

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
            Messenger.Default.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = await _redditService.GetSubreddit(_linkThing.Data.Subreddit) });
        }

		private void GotoUserImpl()
        {
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
            if (vm == null || vm._linkThing == null || vm._linkThing.Data == null || string.IsNullOrWhiteSpace(vm._linkThing.Data.Url))
                vm._baconProvider.GetService<INotificationService>().CreateNotification("Invalid link data, please PM /u/hippiehunter with details");
            else
                vm._navigationService.Navigate(vm._dynamicViewLocator.CommentsView, new SelectCommentTreeMessage { LinkThing = vm._linkThing });
        }

        private static void GotoLinkImpl(LinkViewModel vm)
        {            
            UtilityCommandImpl.GotoLinkImpl(vm.Url, vm._linkThing);
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
