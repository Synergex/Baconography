using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Model.Reddit.ListingHelpers;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using BaconographyWP8.Messages;
using BaconographyWP8.ViewModel;
using BaconographyWP8.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class MainPageViewModel : ViewModelBase
    {
        IBaconProvider _baconProvider;
        IRedditService _redditService;
        IDynamicViewLocator _dynamicViewLocator;
        INavigationService _navigationService;
        IUserService _userService;
        ILiveTileService _liveTileService;
        IOfflineService _offlineService;
        ISettingsService _settingsService;
		INotificationService _notificationService;
        bool _initialLoad = true;


		public MainPageViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = baconProvider.GetService<IRedditService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();
            _navigationService = baconProvider.GetService<INavigationService>();
            _userService = baconProvider.GetService<IUserService>();
            _liveTileService = baconProvider.GetService<ILiveTileService>();
            _offlineService = baconProvider.GetService<IOfflineService>();
            _settingsService = baconProvider.GetService<ISettingsService>();
			_notificationService = baconProvider.GetService<INotificationService>();

			MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
            MessengerInstance.Register<SelectSubredditMessage>(this, OnSubredditChanged);
			MessengerInstance.Register<SelectTemporaryRedditMessage>(this, OnSelectTemporarySubreddit);
			MessengerInstance.Register<CloseSubredditMessage>(this, OnCloseSubreddit);
			MessengerInstance.Register<ReorderSubredditMessage>(this, OnReorderSubreddit);
			MessengerInstance.Register<SettingsChangedMessage>(this, OnSettingsChanged);

			_subreddits = new ObservableCollection<TypedThing<Subreddit>>();

            _pivotItems = new RedditViewModelCollection();
        }

		private async void OnSettingsChanged(SettingsChangedMessage message)
		{
			if (!message.InitialLoad)
				await _baconProvider.GetService<ISettingsService>().Persist();
		}

        bool _currentlySavingSubreddits = false;
        bool _suspendSaving = false;
        async void _subreddits_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(_suspendSaving)
                return;

            int retryCount = 0;
            while (_currentlySavingSubreddits && retryCount++ < 10)
            {
                await Task.Delay(100);
            }

            if (_currentlySavingSubreddits)
                return;

            try
            {
                _currentlySavingSubreddits = true;
                await SaveSubreddits();
            }
            catch { }
            finally
            {
                _currentlySavingSubreddits = false;
            }
        }

		private void OnReorderSubreddit(ReorderSubredditMessage message)
		{
            if (PivotItems != null && Subreddits != null)
            {
                _suspendSaving = true;
                var redditVMs = PivotItems.Select(piv => piv is RedditViewModel ? piv as RedditViewModel : null).ToArray();
                for (int i = Subreddits.Count - 1; i >= 0; i--)
                {
                    if (redditVMs.Length > i && Subreddits[i].Data != null && redditVMs[i].Heading == Subreddits[i].Data.DisplayName)
                        continue;
                    else
                    {
                        var pivot = redditVMs.FirstOrDefault(rvm => Subreddits[i].Data != null && rvm.Heading == Subreddits[i].Data.DisplayName);
                        if (pivot != null)
                        {
                            PivotItems.Remove(pivot);
                            PivotItems.Insert(0, pivot);
                        }
                    }
                }
                _suspendSaving = false;
                _subreddits_CollectionChanged(null, null);
            }
		}

		private void OnCloseSubreddit(CloseSubredditMessage message)
		{
			string heading = message.Heading;
			if (message.Subreddit != null)
			{
				heading = message.Subreddit.Data.DisplayName;
			}

			if (!String.IsNullOrEmpty(heading))
			{
				
				var match = PivotItems.FirstOrDefault(vmb => vmb is RedditViewModel && (vmb as RedditViewModel).Heading == heading) as RedditViewModel;
				if (match != null)
				{
					var subreddit = (match as RedditViewModel).SelectedSubreddit;
					PivotItems.Remove(match);
					RaisePropertyChanged("PivotItems");
                    if (!match.IsTemporary)
                    {
                        _subreddits.Remove(subreddit);
                        RaisePropertyChanged("Subreddits");
                    }
				}
				
			}
		}

		private async void OnUserLoggedIn(UserLoggedInMessage message)
		{
			bool wasLoggedIn = LoggedIn;
			LoggedIn = message.CurrentUser != null && !string.IsNullOrWhiteSpace(message.CurrentUser.LoginCookie);

            if(message.UserTriggered)
			    SubscribedSubreddits.Refresh();

            if (_initialLoad)
            {
                await LoadSubreddits();
                _initialLoad = false;
            }
		}

		private void OnSelectTemporarySubreddit(SelectTemporaryRedditMessage message)
		{
            int indexToPosition;
            bool foundExisting = FindSubredditMessageIndex(message, out indexToPosition);

            if (!foundExisting)
            {
                var newReddit = new RedditViewModel(_baconProvider);
                newReddit.IsTemporary = true;
                newReddit.DetachSubredditMessage();
                newReddit.AssignSubreddit(message);
                if (PivotItems.Count > 0)
                    PivotItems.Insert(PivotItems.Count, newReddit);
                else
                    PivotItems.Add(newReddit);

				indexToPosition = PivotItems.Count - 1;
				RaisePropertyChanged("Subreddits");
            }

			Messenger.Default.Send<SelectIndexMessage>(
				new SelectIndexMessage
				{
					TypeContext = typeof(MainPageViewModel),
					Index = indexToPosition
				}
			);
		}

        public bool FindSubredditMessageIndex(SelectSubredditMessage message, out int indexToPosition)
        {
            indexToPosition = 0;
            foreach (var vm in PivotItems)
            {
                if (vm is RedditViewModel)
                {
                    if (((RedditViewModel)vm).Url == message.Subreddit.Data.Url)
                    {
                        return true;
                    }

                }
                indexToPosition++;
            }
            return false;
        }

		private void OnSubredditChanged(SelectSubredditMessage message)
		{
			ChangeSubreddit(message, !message.AddOnly);
		}

        private void ChangeSubreddit(SelectSubredditMessage message, bool fireSubredditsChanged = true)
        {
            int indexToPosition;
            bool foundExisting = FindSubredditMessageIndex(message, out indexToPosition);

            if (!foundExisting)
            {
                var newReddit = new RedditViewModel(_baconProvider);
                newReddit.DetachSubredditMessage();
                newReddit.AssignSubreddit(message);
                
                if (PivotItems.Count > 0)
                    PivotItems.Insert(PivotItems.Count, newReddit);
                else
                    PivotItems.Add(newReddit);
                _subreddits.Add(message.Subreddit);
                indexToPosition = PivotItems.Count - 1;
            }

            if (fireSubredditsChanged)
            {
                RaisePropertyChanged("Subreddits");

                Messenger.Default.Send<SelectIndexMessage>(
                    new SelectIndexMessage
                    {
                        TypeContext = typeof(MainPageViewModel),
                        Index = indexToPosition
                    }
                );
            }
        }

		public async Task SaveSubreddits()
		{
            try
            {
                await _offlineService.StoreOrderedThings("pivotsubreddits", Subreddits);
            }
            catch { }

		}

		public async Task LoadSubreddits()
		{
            try
            {
                var subreddits = await _offlineService.RetrieveOrderedThings("pivotsubreddits", TimeSpan.FromDays(1024));

                //PivotItems.Add(new SubredditSelectorViewModel(_baconProvider));

                if (subreddits == null || subreddits.Count() == 0)
                    subreddits = new List<TypedThing<Subreddit>> { new TypedThing<Subreddit>(ThingUtility.GetFrontPageThing()) };

                foreach (var sub in subreddits)
                {
                    if (sub.Data is Subreddit && (((Subreddit)sub.Data).Id != null || ((Subreddit)sub.Data).Url.Contains("/m/")))
                    {
                        var message = new SelectSubredditMessage();
                        message.Subreddit = new TypedThing<Subreddit>(sub);
                        message.DontRefresh = true;
                        ChangeSubreddit(message, false);
                    }
                }


                _subreddits.CollectionChanged += _subreddits_CollectionChanged;

                Messenger.Default.Send<SelectIndexMessage>(
                    new SelectIndexMessage
                    {
                        TypeContext = typeof(MainPageViewModel),
                        Index = 0
                    }
                );
            }
            catch 
            {
                _notificationService.CreateNotification("Failed loading subreddits list, file corruption may be present");
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

		public ObservableCollection<TypedThing<Subreddit>> _subreddits;
		public ObservableCollection<TypedThing<Subreddit>> Subreddits
		{
			get
			{
				return _subreddits;
			}
			set
			{
				_subreddits = value;
				RaisePropertyChanged("Subreddits");
			}
		}

        private RedditViewModelCollection _pivotItems;
        public RedditViewModelCollection PivotItems
        {
            get
            {
                return _pivotItems;
            }
        }

		private SubscribedSubredditViewModelCollection _subscribedSubreddits;
		public SubscribedSubredditViewModelCollection SubscribedSubreddits
		{
			get
			{
				if (_subscribedSubreddits == null)
				{
					_subscribedSubreddits = new SubscribedSubredditViewModelCollection(_baconProvider);
				}
				return _subscribedSubreddits;
			}
		}
    }
}
