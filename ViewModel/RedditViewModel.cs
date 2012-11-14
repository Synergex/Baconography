using Callisto.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Baconography.Common;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Actions;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Baconography.ViewModel
{
    public class RedditViewModel : ViewModelBase
    {
        IRedditActionQueue _actionQueue;
        INavigationService _nav;
        IUsersService _userService;
        TypedThing<Subreddit> _selectedSubreddit;

        public RedditViewModel(IRedditActionQueue actionQueue, INavigationService nav, IUsersService userService)
        {
            _actionQueue = actionQueue;
            _nav = nav;
            _userService = userService;
			
            MessengerInstance.Register<UserLoggedIn>(this, (userLoggedIn) =>
            {
                LoggedIn = userLoggedIn.CurrentUser != null && userLoggedIn.CurrentUser.Me != null;
            });

            MessengerInstance.Register<ConnectionStatusMessage>(this, (connection) =>
                {
                    if (IsOnline != connection.IsOnline)
                    {
                        _isOnline = connection.IsOnline;
                        RaisePropertyChanged("IsOnline");
                    }
                });

            MessengerInstance.Register<SelectSubreddit>(this, async (selectSubredit) =>
                {
                    if (_selectedSubreddit == null && selectSubredit == null)
                        return;
                    if (selectSubredit != null)
                    {
                        if (selectSubredit.Subreddit == _selectedSubreddit)
                        {
                            return;
                        }

                        _selectedSubreddit = selectSubredit.Subreddit;
                        SelectedLink = null;
						string targetUrl = "http://www.reddit.com";
						if (_selectedSubreddit != null)
							targetUrl += _selectedSubreddit.Data.Url;
						else
							targetUrl += "/";

						RefreshLinks(targetUrl);

                        Heading = string.Format("{0}: {1}", _selectedSubreddit.Data.Url, _selectedSubreddit.Data.DisplayName);

                        RaisePropertyChanged("DisplayingSubreddit");
                        var currentUser = await _userService.GetUser();
                        if (currentUser != null && currentUser.Me != null)
                        {
                            _subscribed = (await currentUser.SubscribedSubreddits()).Contains(_selectedSubreddit.Data.Name);
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
                });

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

		private void RefreshLinks(string targetUrl)
		{
			_links = new LinkViewModelCollection
			{
				ActionQueue = _actionQueue,
				TargetListing = new Listing { Data = new ListingData { Children = new List<Thing>() } },
				BaseListingUrl = targetUrl,
				UserService = _userService,
				NavigationService = _nav
			};
			RaisePropertyChanged("Links");
		}

        public class LinkViewModelCollection : ObservableCollection<LinkViewModel>, ISupportIncrementalLoading
        {
            public IRedditActionQueue ActionQueue { get; set; }
            public Listing TargetListing { get; set; }
            public string BaseListingUrl { get; set; }
            public IUsersService UserService { get; set; }
            public INavigationService NavigationService { get; set; }
            private HashSet<string> _dupChecker = new HashSet<string>();
            bool _dead = false;

            public bool HasMoreItems
            {
                //have a good url or are currently uninitialized
                get { return !_dead && (TargetListing.Data.After != null || TargetListing.Data.Children.Count == 0); }
            }

            public async Task<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                var currentUser = await UserService.GetUser();

                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                var getAdditional = new GetAdditionalFromListing { BaseURL = BaseListingUrl, After = TargetListing.Data.After };

                if (!currentUser.IsOnline && (getAdditional.After == null || !getAdditional.After.StartsWith("#")))
                {
                    getAdditional.After = "#l";
                }

                var newListing = await getAdditional.Run(currentUser);

                if (newListing.Data.Children.Count == 0)
                    _dead = true;

                //OfflineStore.Links.GetInstance().ContinueWith(async (inst) => (await inst).StoreLinks(newListing));

				LiveTileManager.StartUpdateSequence();
                foreach (var listing in newListing.Data.Children)
                {
                    var linkId = ((Link)listing.Data).Name;
                    if (!_dupChecker.Contains(linkId))
                    {
                        _dupChecker.Add(linkId);
                        Add(new LinkViewModel(listing, ActionQueue, NavigationService));
                    }
                    LiveTileManager.MaybeCreateTile(listing);
                }
                TargetListing = newListing;
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                return new LoadMoreItemsResult { Count = (uint)newListing.Data.Children.Count };
            }

            Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> ISupportIncrementalLoading.LoadMoreItemsAsync(uint count)
            {
                return AsyncInfo.Run((c) => LoadMoreItemsAsync(count));
            }
        }

        LinkViewModelCollection _links;

        public LinkViewModelCollection Links
        {
            get
            {
                if (_links == null)
                {
                    _links = new LinkViewModelCollection
                    {
                        ActionQueue = _actionQueue,
                        TargetListing = new Listing { Data = new ListingData { Children = new List<Thing>() } },
                        BaseListingUrl = "http://www.reddit.com/",
                        UserService = _userService,
                        NavigationService = _nav
                    };
                }
                return _links;
            }
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
                        _selectedLink.NavigateToComments.Execute(null);
                    else
                        _selectedLink.GotoLink.Execute(null);
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
                return Links.BaseListingUrl.StartsWith("http://www.reddit.com/r/");
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

		public bool IsPinned
		{
			get
			{
				if (_selectedSubreddit == null)
					return LiveTileManager.TileExists("");
				return LiveTileManager.TileExists(_selectedSubreddit.Data.Name);
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

        RelayCommand _showSubreddits;
        public RelayCommand ShowSubreddits
        {
            get
            {
                if (_showSubreddits == null)
                {
                    _showSubreddits = new RelayCommand(() =>
                    {
                        _nav.Navigate<Baconography.View.SubredditsView>(null);
                    });
                }
                return _showSubreddits;
            }
        }

		private RelayCommand _pinReddit;
		public RelayCommand PinReddit
		{
			get
			{
				if (_pinReddit == null)
				{
					_pinReddit = new RelayCommand(() =>
					{
						LiveTileManager.CreateSecondaryTileForSubreddit(_selectedSubreddit);
						IsPinned = true;
					});
				}
				return _pinReddit;
			}
		}

		private RelayCommand _unpinReddit;
		public RelayCommand UnpinReddit
		{
			get
			{
				if (_unpinReddit == null)
				{
					_unpinReddit = new RelayCommand(() =>
					{
						if (_selectedSubreddit == null)
							LiveTileManager.RemoveSecondaryTile("");
						else
							LiveTileManager.RemoveSecondaryTile(_selectedSubreddit.Data.DisplayName);
						IsPinned = false;
					});
				}
				return _unpinReddit;
			}
		}

        private RelayCommand _searchReddits;
        public RelayCommand SearchReddits
        {
            get
            {
                if ( _searchReddits == null )
                {
                    _searchReddits = new RelayCommand( () =>
                    {
                        var flyout = new SettingsFlyout();
                        flyout.Content = new Baconography.View.SearchQueryControl();
                        flyout.HeaderText = "Search";
                        flyout.IsOpen = true;
                        flyout.Closed += (e, sender) => MessengerInstance.Unregister<CloseSettingsMessage>( this );
                        MessengerInstance.Register<CloseSettingsMessage>(this, (message) =>
                        {
                            flyout.IsOpen = false;
                        });
                    });
                }
                return _searchReddits;
            }
        }

        RelayCommand _subscribeSubreddit;
        public RelayCommand SubscribeSubreddit
        {
            get
            {
                if (_subscribeSubreddit == null)
                {
                    _subscribeSubreddit = new RelayCommand(() =>
                        {
                            _actionQueue.AddAction(new AddSubredditSubscription { Unsub = false });
                        });
                }
                return _subscribeSubreddit;
            }
        }


        RelayCommand _unsubscribeSubreddit;
        public RelayCommand UnsubscribeSubreddit
        {
            get
            {
                if (_unsubscribeSubreddit == null)
                {
                    _unsubscribeSubreddit = new RelayCommand(() =>
                    {
                        _actionQueue.AddAction(new AddSubredditSubscription { Unsub = true });
                    });
                }
                return _unsubscribeSubreddit;
            }
        }


        RelayCommand _submitToSubreddit;
        public RelayCommand SubmitToSubreddit
        {
            get
            {
                if (_submitToSubreddit == null)
                {
                    _submitToSubreddit = new RelayCommand(() =>
                        {
                            //_nav.Navigate<SubmitToSubreddit>();
                        });
                }
                return _submitToSubreddit;
            }
        }

		RelayCommand _goOffline;
		public RelayCommand GoOffline
		{
			get
			{
				if (_goOffline == null)
				{
					_goOffline = new RelayCommand(() =>
					{
						this.IsOnline = false;
					});
				}
				return _goOffline;
			}
		}

		RelayCommand _goOnline;
		public RelayCommand GoOnline
		{
			get
			{
				if (_goOnline == null)
				{
					_goOnline = new RelayCommand(() =>
					{
						this.IsOnline = true;
					});
				}
				return _goOnline;
			}
		}

		RelayCommand _refreshRedditView;
		public RelayCommand RefreshRedditView
		{
			get
			{
				if (_refreshRedditView == null)
				{
					_refreshRedditView = new RelayCommand(() =>
					{
						MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = true });

						var targetUrl = "http://www.reddit.com";
						if (_selectedSubreddit != null)
							targetUrl += _selectedSubreddit.Data.Url;
						else
							targetUrl += "/";

						// TODO: If currently scrolled, refreshing the links causes the scrollbar to be top aligned,
						// but the content is correctly scrolled to the user's last position
						RefreshLinks(targetUrl);

						MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = false });
					});
				}
				return _refreshRedditView;
			}
		}

        RelayCommand _downloadForOffline;
        public RelayCommand DownloadForOffline
        {
            get
            {
                if (_downloadForOffline == null)
                {
                    _downloadForOffline = new RelayCommand(async () =>
                        {
                            MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = true });

                            var targetUrl = "http://www.reddit.com";
                            if (_selectedSubreddit != null)
                            {
                                targetUrl += _selectedSubreddit.Data.Url;
                            }
                            else
                                targetUrl += "/";

                            var offlineLinks = new MakeOfflineLinks { BaseUrl = targetUrl, Limit = 25 };

                            await offlineLinks.Run(await _userService.GetUser());

                            MessengerInstance.Send<LoadingMessage>(new LoadingMessage { Loading = false });
							_offlineReady = true;
							RaisePropertyChanged("OfflineReady");
                        });
                }
                return _downloadForOffline;
            }
        }

    }
    
}
