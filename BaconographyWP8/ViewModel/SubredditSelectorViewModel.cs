using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class SubredditSelectorViewModel : ViewModelBase
    {
        IRedditService _redditService;
        INavigationService _navigationService;
        IUserService _userService;
        IDynamicViewLocator _dynamicViewLocator;
        IBaconProvider _baconProvider;
        ISystemServices _systemServices;

		public SubredditSelectorViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();
            _systemServices = _baconProvider.GetService<ISystemServices>();
            Subreddits = new SubredditViewModelCollection(_baconProvider);
            _nonSearchSubreddits = Subreddits;
        }

		private string _text;
		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
                bool wasChanged = _text != value;
                if (wasChanged)
                {
                    _text = value;
                    RaisePropertyChanged("Text");

                    if (_text.Length < 3)
                    {
                        if (_subreddits != _nonSearchSubreddits)
                            Subreddits = _nonSearchSubreddits;
                        RevokeQueryTimer();
                    }
                    else
                    {
                        RestartQueryTimer();
                    }
                }
			}
		}
        Object _queryTimer;
        void RevokeQueryTimer()
        {
            if (_queryTimer != null)
            {
                _systemServices.StopTimer(_queryTimer);
                _queryTimer = null;
            }
        }

        void RestartQueryTimer()
        {
            // Start or reset a pending query
            if (_queryTimer == null)
            {
                _queryTimer = _systemServices.StartTimer(queryTimer_Tick, new TimeSpan(0, 0, 1), true);
            }
            else
            {
                _systemServices.StopTimer(_queryTimer);
                _systemServices.RestartTimer(_queryTimer);
            }
        }

        void queryTimer_Tick(object sender, object timer)
        {
            // Stop the timer so it doesn't fire again unless rescheduled
            RevokeQueryTimer();
            Subreddits = new SearchResultsViewModelCollection(_baconProvider, _text, true);
        }

        public AboutSubredditViewModel SelectedSubreddit
        {
            get
            {
                return null;
            }
            set
            {
				var message = new SelectSubredditMessage { Subreddit = value.Thing };
				MessengerInstance.Send<SelectSubredditMessage>(message);
            }
        }


		public RelayCommand<SubredditSelectorViewModel> PinSubreddit { get { return _pinSubreddit; } }
		static RelayCommand<SubredditSelectorViewModel> _pinSubreddit = new RelayCommand<SubredditSelectorViewModel>(PinSubredditImpl);

		private async static void PinSubredditImpl(SubredditSelectorViewModel vm)
		{
			vm.DoPinSubreddit();
		}

		private async void DoPinSubreddit()
		{
			var subredditName = Text;
			if (String.IsNullOrEmpty(subredditName))
				return;

			if (subredditName.Contains('/') && subredditName != "/")
				subredditName = subredditName.Substring(subredditName.LastIndexOf('/') + 1);

			var _redditService = ServiceLocator.Current.GetInstance<IRedditService>();
			if (_redditService == null)
				return;

			var subreddit = await _redditService.GetSubreddit(subredditName);
            if (subreddit == null)
            {
                return;
            }
            Text = "";
			MessengerInstance.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = subreddit });
		}
        ThingViewModelCollection _nonSearchSubreddits;
        ThingViewModelCollection _subreddits;
        public ThingViewModelCollection Subreddits
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
    }
}
