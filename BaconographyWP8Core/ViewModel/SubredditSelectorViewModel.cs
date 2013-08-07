﻿using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
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
            Subreddits = new BindingShellViewModelCollection(new SubredditViewModelCollection(_baconProvider));
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
                        Subreddits.RevertToDefault();
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
            if (Subreddits != null)
            {
                if(!(_text != null && _text.Contains("/")))
                    Subreddits.UpdateRealItems(new SearchResultsViewModelCollection(_baconProvider, _text, true));
            }
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
			vm.DoGoSubreddit(true);
		}

        private static string CleanRedditLink(string userInput, User user)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return "/";

            if (userInput == "/")
                return userInput;

            if (user != null && !string.IsNullOrWhiteSpace(user.Username))
            {
                var selfMulti = "/" + user.Username + "/m/";
                if (userInput.Contains(selfMulti))
                {
                    return "/me/m/" + userInput.Substring(userInput.IndexOf(selfMulti) + selfMulti.Length);
                }
            }

            if (userInput.StartsWith("me/m/"))
                return "/" + userInput;
            else if (userInput.StartsWith("/m/"))
                return "/me" + userInput;
            else if (userInput.StartsWith("/me/m/"))
                return userInput;

            if (userInput.StartsWith("/u/"))
            {
                return userInput.Replace("/u/", "/user/");
            }

            if (userInput.StartsWith("r/"))
                return "/" + userInput;
            else if (userInput.StartsWith("/") && !userInput.StartsWith("/r/"))
                return "/r" + userInput;
            else if (userInput.StartsWith("/r/"))
                return userInput;
            else
                return "/r/" + userInput;
        }

		public async void DoGoSubreddit(bool pin)
		{
            var subredditName = CleanRedditLink(Text, await _userService.GetUser()).Replace("/r/", "");
			if (String.IsNullOrEmpty(subredditName))
				return;

			var _redditService = ServiceLocator.Current.GetInstance<IRedditService>();
			if (_redditService == null)
				return;

			var subreddit = await _redditService.GetSubreddit(subredditName);
            if (subreddit == null)
            {
                return;
            }
            if(pin)
			    MessengerInstance.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = subreddit });
            else
                MessengerInstance.Send<SelectTemporaryRedditMessage>(new SelectTemporaryRedditMessage { Subreddit = subreddit });
		}

        public BindingShellViewModelCollection Subreddits { get; set; }
    }
}
