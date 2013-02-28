using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Model.Reddit.ListingHelpers;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
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

            MessengerInstance.Register<SelectSubredditMessage>(this, OnSubredditChanged);
			PivotItems = new RedditViewModelCollection(_baconProvider);
			var redditVM = new RedditViewModel(_baconProvider);
			redditVM.DetachSubredditMessage();
			PivotItems.Add(redditVM);
			PivotItems.Add(new SubredditSelectorViewModel(_baconProvider));

			Subreddits = new List<TypedThing<Subreddit>>();

			LoadSubreddits();
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
					var message = new SelectSubredditMessage();
					message.Subreddit = sub;
					OnSubredditChanged(message);
				}
			}
		}

		public List<TypedThing<Subreddit>> Subreddits;

		public RedditViewModelCollection PivotItems { get; private set; }

    }
}
