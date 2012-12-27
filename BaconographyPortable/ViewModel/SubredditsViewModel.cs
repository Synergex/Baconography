﻿using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class SubredditsViewModel : ViewModelBase
    {
        IRedditService _redditService;
        INavigationService _navigationService;
        IUserService _userService;
        IDynamicViewLocator _dynamicViewLocator;
        IBaconProvider _baconProvider;

        public SubredditsViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();

            Subreddits = new SubredditViewModelCollection(_baconProvider);
        }

        public AboutSubredditViewModel SelectedSubreddit
        {
            get
            {
                return null;
            }
            set
            {
                _navigationService.GoBack();
                _navigationService.Navigate(_dynamicViewLocator.RedditView, new SelectSubredditMessage { Subreddit = value.Thing });
            }
        }

        public SubredditViewModelCollection Subreddits { get; private set; }
    }
}
