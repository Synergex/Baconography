﻿using BaconographyPortable.Messages;
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
			PivotItems = new RedditViewModelCollection(_baconProvider);

			_subreddits = new ObservableCollection<TypedThing<Subreddit>>();
        }

		private async void OnReorderSubreddit(ReorderSubredditMessage message)
		{
			var redditVMs = PivotItems.Select(piv => piv is RedditViewModel ? piv as RedditViewModel : null);
			for (int i = Subreddits.Count - 1; i >= 0 ; i--)
			{
				var pivot = redditVMs.FirstOrDefault(rvm => rvm.Heading == Subreddits[i].Data.DisplayName);
				if (pivot != null)
				{
					PivotItems.Remove(pivot);
					PivotItems.Insert(0, pivot);
				}
			}
		}

		private async void OnCloseSubreddit(CloseSubredditMessage message)
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

		private void OnUserLoggedIn(UserLoggedInMessage message)
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
                _initialLoad = false;
                LoadSubreddits();
            }
		}

		private async void OnSelectTemporarySubreddit(SelectTemporaryRedditMessage message)
		{
			var newReddit = new TemporaryRedditViewModel(_baconProvider);
			newReddit.RedditViewModel.DetachSubredditMessage();
			newReddit.RedditViewModel.AssignSubreddit(message);
			PivotItems.Add(newReddit);
			RaisePropertyChanged("PivotItems");
			Messenger.Default.Send<SelectIndexMessage>(
				new SelectIndexMessage
				{
					TypeContext = typeof(MainPageViewModel),
					Index = PivotItems.Count - 1
				}
			);
		}

		private async void OnSubredditChanged(SelectSubredditMessage message)
		{
			await ChangeSubreddit(message);
		}

        private async Task ChangeSubreddit(SelectSubredditMessage message, bool fireSubredditsChanged = true)
        {
			var newReddit = new RedditViewModel(_baconProvider);
			newReddit.DetachSubredditMessage();
			newReddit.AssignSubreddit(message);
            if (PivotItems.Count > 0)
                PivotItems.Insert(PivotItems.Count - 1, newReddit);
            else
                PivotItems.Add(newReddit);
			_subreddits.Add(message.Subreddit);
			if (fireSubredditsChanged)
				RaisePropertyChanged("Subreddits");
			RaisePropertyChanged("PivotItems");
			Messenger.Default.Send<SelectIndexMessage>(
				new SelectIndexMessage
				{
					TypeContext = typeof(MainPageViewModel),
					Index = PivotItems.Count - 2
				}
			);
        }

		public async Task SaveSubreddits()
		{
			var serializedSubreddits = JsonConvert.SerializeObject(Subreddits);
			await _offlineService.StoreSetting("pivotsubreddits", serializedSubreddits);
		}

		public async void LoadSubreddits()
		{
            PivotItems.Add(new SubredditSelectorViewModel(_baconProvider));
			List<TypedThing<Subreddit>> subreddits = null;

			var serializedSubreddits = await _offlineService.GetSetting("pivotsubreddits");
			if (serializedSubreddits != null)
			{
				subreddits = JsonConvert.DeserializeObject<List<TypedThing<Subreddit>>>(serializedSubreddits);
			}

			if (subreddits == null || subreddits.Count == 0)
				subreddits = new List<TypedThing<Subreddit>> { new TypedThing<Subreddit>(SubredditInfo.GetFrontPageThing()) };
                
            foreach (var sub in subreddits)
            {
                if (sub.Data != null && sub.Data.Id != null)
                {
                    var message = new SelectSubredditMessage();
                    message.Subreddit = sub;
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
				Task.WaitAll(SaveSubreddits());
			}
		}

		public RedditViewModelCollection PivotItems { get; private set; }

    }
}
