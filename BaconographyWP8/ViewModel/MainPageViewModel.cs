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
        bool _initialLoad = true;
        WeakReference<Task> _subredditSavingTask;


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

			MessengerInstance.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
            MessengerInstance.Register<SelectSubredditMessage>(this, OnSubredditChanged);
			MessengerInstance.Register<SelectTemporaryRedditMessage>(this, OnSelectTemporarySubreddit);
			MessengerInstance.Register<CloseSubredditMessage>(this, OnCloseSubreddit);
			MessengerInstance.Register<ReorderSubredditMessage>(this, OnReorderSubreddit);
			_pivotItems = new RedditViewModelCollection(_baconProvider);

			_subreddits = new ObservableCollection<TypedThing<Subreddit>>();
            _subreddits.CollectionChanged += _subreddits_CollectionChanged;
        }

        void _subreddits_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!_initialLoad)
            {
                Task currentTask;
                if (_subredditSavingTask != null && _subredditSavingTask.TryGetTarget(out currentTask))
                {
                    currentTask.ContinueWith(async (o) => await SaveSubreddits());
                }
                else
                    _subredditSavingTask = new WeakReference<Task>(SaveSubreddits());
            }
        }

		private void OnReorderSubreddit(ReorderSubredditMessage message)
		{
            if (PivotItems != null && Subreddits != null)
            {
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
				var match = PivotItems.FirstOrDefault(vmb => vmb is TemporaryRedditViewModel && (vmb as TemporaryRedditViewModel).RedditViewModel.Heading == heading);
				if (match != null)
				{
					PivotItems.Remove(match);
					RaisePropertyChanged("PivotItems");
				}
				else
				{
					match = PivotItems.FirstOrDefault(vmb => vmb is RedditViewModel && (vmb as RedditViewModel).Heading == heading);
					if (match != null)
					{
						var subreddit = (match as RedditViewModel).SelectedSubreddit;
						PivotItems.Remove(match);
						RaisePropertyChanged("PivotItems");
						_subreddits.Remove(subreddit);
						RaisePropertyChanged("Subreddits");
					}
				}
			}
		}

		private async void OnUserLoggedIn(UserLoggedInMessage message)
		{
			bool wasLoggedIn = LoggedIn;
			LoggedIn = message.CurrentUser != null && message.CurrentUser.Me != null;
			if (wasLoggedIn != _loggedIn)
			{
                if (PivotItems.Count > 0 && PivotItems[0] != null && PivotItems[0] is RedditViewModel)
					(PivotItems[0] as RedditViewModel).RefreshLinks();
			}

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
                var newReddit = new TemporaryRedditViewModel(_baconProvider);
                newReddit.RedditViewModel.DetachSubredditMessage();
                newReddit.RedditViewModel.AssignSubreddit(message);
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
                else if (vm is TemporaryRedditViewModel)
                {
                    if (((TemporaryRedditViewModel)vm).RedditViewModel.Url == message.Subreddit.Data.Url)
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
			ChangeSubreddit(message);
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
                RaisePropertyChanged("PivotItems");
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
            await _offlineService.StoreOrderedThings("pivotsubreddits", Subreddits);
		}

		public async Task LoadSubreddits()
		{
            var subreddits = await _offlineService.RetrieveOrderedThings("pivotsubreddits");

            //PivotItems.Add(new SubredditSelectorViewModel(_baconProvider));

			if (subreddits == null || subreddits.Count() == 0)
				subreddits = new List<TypedThing<Subreddit>> { new TypedThing<Subreddit>(SubredditInfo.GetFrontPageThing()) };
                
            foreach (var sub in subreddits)
            {
                if (sub.Data is Subreddit && ((Subreddit)sub.Data).Id != null)
                {
                    var message = new SelectSubredditMessage();
                    message.Subreddit = new TypedThing<Subreddit>(sub);
					ChangeSubreddit(message, false);
                }
            }

			Messenger.Default.Send<SelectIndexMessage>(
				new SelectIndexMessage
				{
					TypeContext = typeof(MainPageViewModel),
					Index = 0
				}
			);
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
