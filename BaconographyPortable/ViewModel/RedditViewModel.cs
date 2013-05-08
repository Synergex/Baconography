using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Model.Reddit.ListingHelpers;
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
    public class RedditViewModel : ViewModelBase
    {
        IBaconProvider _baconProvider;
        IRedditService _redditService;
        IDynamicViewLocator _dynamicViewLocator;
        INavigationService _navigationService;
        IUserService _userService;
        ILiveTileService _liveTileService;
        IOfflineService _offlineService;
        ISettingsService _settingsService;
        TypedThing<Subreddit> _selectedSubreddit; 

        public RedditViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = baconProvider.GetService<IRedditService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();
            _navigationService = baconProvider.GetService<INavigationService>();
            _userService = baconProvider.GetService<IUserService>();
            _liveTileService = baconProvider.GetService<ILiveTileService>();
            _offlineService = baconProvider.GetService<IOfflineService>();
            _settingsService = baconProvider.GetService<ISettingsService>();

            MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
            MessengerInstance.Register<ConnectionStatusMessage>(this, OnConnectionStatusChanged);
            MessengerInstance.Register<SelectSubredditMessage>(this, OnSubredditChanged);
			MessengerInstance.Register<RefreshSubredditMessage>(this, OnSubredditRefreshed);
        }

		public void DetachSubredditMessage()
		{
			MessengerInstance.Unregister<SelectSubredditMessage>(this);
		}

		public void AssignSubreddit(SelectSubredditMessage message)
		{
			OnSubredditChanged(message);
		}

        private void OnUserLoggedIn(UserLoggedInMessage message)
        {
            if (message.UserTriggered && Url == "/")
                RefreshLinks();

            LoggedIn = message.CurrentUser != null && message.CurrentUser.Me != null;
        }

        private void OnConnectionStatusChanged(ConnectionStatusMessage message)
        {
            if (IsOnline != message.IsOnline)
            {
                _isOnline = message.IsOnline;
                RaisePropertyChanged("IsOnline");
            }
        }

		private async void OnSubredditRefreshed(RefreshSubredditMessage message)
		{
			if (this.SelectedSubreddit == message.Subreddit)
			{
				RefreshLinks();
			}
		}

        private async void OnSubredditChanged(SelectSubredditMessage message)
        {
            if (_selectedSubreddit == null && message == null)
                return;
            if (message != null)
            {
                if (message.Subreddit == _selectedSubreddit)
                {
                    return;
                }

                _selectedSubreddit = message.Subreddit;
                SelectedLink = null;
                RefreshLinks();

				Heading = _selectedSubreddit.Data.DisplayName;

                RaisePropertyChanged("DisplayingSubreddit");
                var currentUser = await _userService.GetUser();
                if (currentUser != null && currentUser.Me != null)
                {
                    _subscribed = (await _redditService.GetSubscribedSubreddits()).Contains(_selectedSubreddit.Data.Name);
                    RaisePropertyChanged("NotSubscribed");
                    RaisePropertyChanged("Subscribed");
                }
            }
            else
            {
                //set us back to default state
                _selectedSubreddit = null;
                SelectedLink = null;
                _links = null;
                Heading = null;
                RaisePropertyChanged("DisplayingSubreddit");
                _subscribed = false;
                RaisePropertyChanged("NotSubscribed");
                RaisePropertyChanged("Subscribed");

            }
        }

        private bool _isOnline = true;
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
                RaisePropertyChanged("OfflineReady");
                MessengerInstance.Send<ConnectionStatusMessage>(new ConnectionStatusMessage { IsOnline = value, UserInitiated = true });
            }
        }

        private bool _offlineReady = false;
        public bool OfflineReady
        {
            get
            {
                return IsOnline && _offlineReady;
            }
        }

        public void RefreshLinks()
        {
            Links.Refresh();
        }

        LinkViewModelCollection _links;

        public LinkViewModelCollection Links
        {
            get
            {
                if (_links == null)
                {
                    _links = LinksImpl();
                }
                return _links;
            }
        }

        private LinkViewModelCollection LinksImpl()
        {
            string subreddit = "/", subredditId = null;
            if(_selectedSubreddit != null)
            {
				subreddit = _selectedSubreddit.Data.Url + _sortOrder;
                subredditId = _selectedSubreddit.Data.Name;
            }

            return new LinkViewModelCollection(_baconProvider, subreddit, subredditId);
        }


        LinkViewModel _selectedLink;
        public LinkViewModel SelectedLink
        {
            get
            {
                return _selectedLink;
            }
            set
            {
                _selectedLink = value;
                RaisePropertyChanged("SelectedLink");
                if (_selectedLink != null)
                {
                    if (SelectedLink.IsSelfPost)
                        _selectedLink.NavigateToComments.Execute(value);
                    else
                        _selectedLink.GotoLink.Execute(value);
                }
            }
        }

        Nullable<bool> _subscribed;
        public bool NotSubscribed
        {
            get
            {
                return _subscribed ?? false;
            }
            set
            {
                _subscribed = !value;
                RaisePropertyChanged("NotSubscribed");
                RaisePropertyChanged("Subscribed");
            }
        }

        public bool Subscribed
        {
            get
            {
                return _subscribed ?? false;
            }
            set
            {
                _subscribed = value;
                RaisePropertyChanged("NotSubscribed");
                RaisePropertyChanged("Subscribed");
            }
        }

        public bool DisplayingSubreddit
        {
            get
            {
                return _selectedSubreddit != null;
            }
        }

		public TypedThing<Subreddit> SelectedSubreddit
		{
			get
			{
				return _selectedSubreddit;
			}
		}

        private string _heading;
        public string Heading
        {
            get
            {
                if (_heading == null)
                    _heading = "The front page of this device";
                return _heading;
            }
            set
            {
                _heading = value;
                RaisePropertyChanged("Heading");
            }
        }

		public bool IsFrontPage
		{
			get
			{
				if (_selectedSubreddit == null || _selectedSubreddit.Data.Url == "/")
					return true;
				return false;
			}
		}

		public bool IsTilePinned
        {
            get
            {
                if (_selectedSubreddit == null)
                    return _liveTileService.TileExists("");
                return _liveTileService.TileExists(_selectedSubreddit.Data.Name);
            }
            set
            {
                RaisePropertyChanged("IsPinned");
            }
        }

        private bool _loggedIn;
        public bool LoggedIn
        {
            get
            {
                return _loggedIn;
            }
            set
            {
                _loggedIn = value;
                try
                {
                    RaisePropertyChanged("LoggedIn");
                }
                catch { }
            }
        }

		private string _sortOrder = "";
		//  hot - ""
		// /new/
		// /controversial/
		// /top/
		// /rising/
		public string SortOrder
		{
			get
			{
				return _sortOrder;
			}
			set
			{
				string orig = _sortOrder;
				switch (value)
				{
					case "new":
					case "top":
					case "rising":
					case "controversial":
						_sortOrder = "/" + value + "/";
						break;

					case "":
					case "hot":
					default:
						_sortOrder = "";
						break;
				}

				if (_sortOrder != orig)
				{
					_links = LinksImpl();
					RaisePropertyChanged("Links");
					RaisePropertyChanged("SortOrder");
				}
			}
		}

        public string Url
        {
            get
            {
                if (_selectedSubreddit == null)
                    return "/";
                else
                    return _selectedSubreddit.Data.Url;
            }
        }

        public static RelayCommand<RedditViewModel> ShowSubreddits { get { return _showSubreddits; } }
        public static RelayCommand<RedditViewModel> PinReddit { get { return _pinReddit; } }
        public static RelayCommand<RedditViewModel> UnpinReddit { get { return _unpinReddit; } }
        public static RelayCommand<RedditViewModel> SearchReddits { get { return _searchReddits; } }
        public static RelayCommand<RedditViewModel> SubscribeSubreddit { get { return _subscribeSubreddit; } }
        public static RelayCommand<RedditViewModel> UnsubscribeSubreddit { get { return _unsubscribeSubreddit; } }
        public static RelayCommand<RedditViewModel> SubmitToSubreddit { get { return _submitToSubreddit; } }
        public static RelayCommand<RedditViewModel> GoOffline { get { return _goOffline; } }
        public static RelayCommand<RedditViewModel> GoOnline { get { return _goOnline; } }
        public static RelayCommand<RedditViewModel> RefreshRedditView { get { return _refreshRedditView; } }
        public static RelayCommand<RedditViewModel> DownloadForOffline { get { return _downloadForOffline; } }

        static RelayCommand<RedditViewModel> _showSubreddits = new RelayCommand<RedditViewModel>((vm) => vm.ShowSubredditsImpl());
        static RelayCommand<RedditViewModel> _pinReddit = new RelayCommand<RedditViewModel>((vm) => vm.PinRedditImpl());
        static RelayCommand<RedditViewModel> _unpinReddit = new RelayCommand<RedditViewModel>((vm) => vm.UnpinRedditImpl());
        static RelayCommand<RedditViewModel> _searchReddits = new RelayCommand<RedditViewModel>((vm) => vm.SearchRedditsImpl());
        static RelayCommand<RedditViewModel> _subscribeSubreddit = new RelayCommand<RedditViewModel>((vm) => vm.SubscribeSubredditImpl());
        static RelayCommand<RedditViewModel> _unsubscribeSubreddit = new RelayCommand<RedditViewModel>((vm) => vm.UnsubscribeSubredditImpl());
        static RelayCommand<RedditViewModel> _submitToSubreddit = new RelayCommand<RedditViewModel>((vm) => vm.SubmitToSubredditImpl());
        static RelayCommand<RedditViewModel> _goOffline = new RelayCommand<RedditViewModel>((vm) => vm.GoOfflineImpl());
        static RelayCommand<RedditViewModel> _goOnline = new RelayCommand<RedditViewModel>((vm) => vm.GoOnlineImpl());
        static RelayCommand<RedditViewModel> _refreshRedditView = new RelayCommand<RedditViewModel>((vm) => vm.RefreshLinks());
        static RelayCommand<RedditViewModel> _downloadForOffline = new RelayCommand<RedditViewModel>((vm) => vm.DownloadForOfflineImpl());

        private void ShowSubredditsImpl()
        {
            _navigationService.Navigate(_dynamicViewLocator.SubredditsView, null);
        }

        private void PinRedditImpl()
        {
            _liveTileService.CreateSecondaryTileForSubreddit(_selectedSubreddit);
			IsTilePinned = true;
        }

        private void UnpinRedditImpl()
        {
            if (_selectedSubreddit == null)
                _liveTileService.RemoveSecondaryTile("");
            else
                _liveTileService.RemoveSecondaryTile(_selectedSubreddit.Data.DisplayName);
			IsTilePinned = false;
        }

        private void SearchRedditsImpl()
        {
            _navigationService.NavigateToSecondary(_dynamicViewLocator.SearchQueryView, null);
        }

        private void SubscribeSubredditImpl()
        {
            //TODO: is this the right name?
            var subreddit = _selectedSubreddit != null ? _selectedSubreddit.Data.Name : "";
            _redditService.AddSubredditSubscription(subreddit, false);
        }


        private void UnsubscribeSubredditImpl()
        {
            //TODO: is this the right name?
            var subreddit = _selectedSubreddit != null ? _selectedSubreddit.Data.Name : "";
            _redditService.AddSubredditSubscription(subreddit, false);
        }

        private void SubmitToSubredditImpl()
        {
            _navigationService.Navigate(_dynamicViewLocator.SubmitToSubredditView, null);
        }

        private void GoOfflineImpl()
        {
            IsOnline = false;
        }

        private void GoOnlineImpl()
        {
            IsOnline = true;
        }

        private async void DownloadForOfflineImpl()
        {
            MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = true });

            await _offlineService.StoreLinks(await _redditService.GetPostsBySubreddit(_selectedSubreddit != null ? _selectedSubreddit.Data.Url : "/", _settingsService.DefaultOfflineLinkCount));

            MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            _offlineReady = true;
            RaisePropertyChanged("OfflineReady");
        }
    }
}
