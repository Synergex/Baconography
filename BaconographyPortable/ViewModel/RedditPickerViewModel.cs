﻿using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
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
    public class RedditPickerViewModel : ViewModelBase
    {
        INavigationService _navigationService;
        IDynamicViewLocator _dynamicViewLocator;
        IRedditService _redditService;
        string _targetSubreddit;
        public RedditPickerViewModel(IBaconProvider baconProvider)
        {
            _navigationService = baconProvider.GetService<INavigationService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();
            _redditService = baconProvider.GetService<IRedditService>();

            _subreddits = new SubscribedSubredditViewModelCollection(baconProvider);
            _gotoSubreddit = new RelayCommand(GotoSubredditImpl);
        }

        public string TargetSubreddit
        {
            get
            {
                return _targetSubreddit;
            }
            set
            {
                _targetSubreddit = value;
                RaisePropertyChanged("TargetSubreddit");
            }
        }

        RelayCommand _gotoSubreddit;
        static RelayCommand<RedditPickerViewModel> _showViewAll = new RelayCommand<RedditPickerViewModel>((vm) => vm.ShowViewAllImpl());
        static RelayCommand<RedditPickerViewModel> _showMultiReddit = new RelayCommand<RedditPickerViewModel>((vm) => vm.ShowMultiRedditImpl());
        
        public RelayCommand<RedditPickerViewModel> ShowViewAll { get { return _showViewAll; } }
        public RelayCommand<RedditPickerViewModel> ShowMultiReddit { get { return _showMultiReddit; } }
        public RelayCommand GotoSubreddit { get { return _gotoSubreddit; } }

        private void ShowViewAllImpl()
        {
            _navigationService.Navigate(_dynamicViewLocator.SubredditsView, null);
        }

        private async void GotoSubredditImpl()
        {
            if(string.IsNullOrWhiteSpace(TargetSubreddit))
            {
                _navigationService.Navigate(_dynamicViewLocator.RedditView, null);
            }
            else if(TargetSubreddit.StartsWith("/r/"))
            {
                TargetSubreddit = TargetSubreddit.Substring("/r/".Length);
            }

            _navigationService.Navigate(_dynamicViewLocator.RedditView, new SelectSubredditMessage { Subreddit = await _redditService.GetSubreddit(TargetSubreddit) });
        }

        private void ShowMultiRedditImpl()
        {
            //TODO: implement this
        }

        public AboutSubredditViewModel SelectedSubreddit
        {
            get
            {
                return null;
            }
            set
            {
                _navigationService.Navigate(_dynamicViewLocator.RedditView, new SelectSubredditMessage { Subreddit = new TypedThing<Subreddit>(value.Thing) });
            }
        }

        SubscribedSubredditViewModelCollection _subreddits;
        public SubscribedSubredditViewModelCollection Subreddits
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
