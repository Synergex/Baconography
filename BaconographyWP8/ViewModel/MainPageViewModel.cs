using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Model.Reddit.ListingHelpers;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using BaconographyWP8.Messages;
using BaconographyWP8.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
			MessengerInstance.Register<CloseSubredditMessage>(this, OnCloseSubreddit);
			PivotItems = new RedditViewModelCollection(_baconProvider);
			var redditVM = new RedditViewModel(_baconProvider);
			redditVM.DetachSubredditMessage();
			PivotItems.Add(redditVM);
			PivotItems.Add(new SubredditSelectorViewModel(_baconProvider));

			Subreddits = new List<TypedThing<Subreddit>>();

			LoadSubreddits();
        }

		private void OnCloseSubreddit(CloseSubredditMessage message)
		{
			string heading = message.Heading;
			if (message.Subreddit != null)
			{
				heading = message.Subreddit.Data.Name;
			}

			if (!String.IsNullOrEmpty(message.Heading) &&
				heading != "The front page of this device")
			{
				var match = PivotItems.FirstOrDefault(vmb => vmb is RedditViewModel && (vmb as RedditViewModel).Heading == heading);
				if (match != null)
				{
					var subreddit = (match as RedditViewModel).SelectedSubreddit;
					PivotItems.Remove(match);
					RaisePropertyChanged("PivotItems");
					Subreddits.Remove(subreddit);
					RaisePropertyChanged("Subreddits");
				}
			}
		}

		private void OnUserLoggedIn(UserLoggedInMessage message)
		{
			bool wasLoggedIn = LoggedIn;
			LoggedIn = message.CurrentUser != null && message.CurrentUser.Me != null;
			if (wasLoggedIn != _loggedIn)
			{
				if (PivotItems[0] != null)
					(PivotItems[0] as RedditViewModel).RefreshLinks();
			}
		}

        private async void OnSubredditChanged(SelectSubredditMessage message)
        {
			var newReddit = new RedditViewModel(_baconProvider);
			newReddit.DetachSubredditMessage();
			newReddit.AssignSubreddit(message);
			PivotItems.Insert(PivotItems.Count - 1, newReddit);
			Subreddits.Add(message.Subreddit);
        }

		public async void  SaveSubreddits()
		{
			var serializedSubreddits = JsonConvert.SerializeObject(Subreddits);
			await _offlineService.StoreSetting("pivotsubreddits", serializedSubreddits);
		}

		public async void LoadSubreddits()
		{
			var serializedSubreddits = await _offlineService.GetSetting("pivotsubreddits");
			var subreddits = JsonConvert.DeserializeObject<List<TypedThing<Subreddit>>>(serializedSubreddits);
			if (subreddits != null)
			{
				foreach (var sub in subreddits)
				{
					if (sub.Data != null && sub.Data.Id != null)
					{
						var message = new SelectSubredditMessage();
						message.Subreddit = sub;
						OnSubredditChanged(message);
					}
				}
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

		public List<TypedThing<Subreddit>> Subreddits;

		public RedditViewModelCollection PivotItems { get; private set; }

    }
}
