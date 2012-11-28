using BaconographyPortable.Messages;
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
    public class SearchResultsViewModel : ViewModelBase
    {
        private IRedditService _redditService;
        private INavigationService _navigationService;
        private IUserService _userService;
        private IBaconProvider _baconProvider;
        private IDynamicViewLocator _dynamicViewLocator;

        public SearchResultsViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();

            MessengerInstance.Register<SearchQueryMessage>(this, OnSearchQuery);

        }

        private void OnSearchQuery(SearchQueryMessage queryMessage)
        {
            Query = queryMessage.Query;
            Results = new SearchResultsViewModelCollection(_baconProvider, Query);
        }

        private string _query;
        public string Query
        {
            get
            {
                return _query;
            }
            set
            {
                _query = value;
                RaisePropertyChanged("Query");
                RaisePropertyChanged("Heading");
            }
        }

        public string Heading
        {
            get
            {
                return _query;
            }
        }

        ThingViewModelCollection _results;
        public ThingViewModelCollection Results
        {
            get
            {
                return _results;
            }
            set
            {
                _results = value;
                RaisePropertyChanged("Results");
            }
        }

        public ViewModelBase SelectedItem
        {
            get
            {
                return null;
            }
            set
            {
                if (value is LinkViewModel)
                    ((LinkViewModel)value).GotoLink.Execute(null);
                else if (value is AboutSubredditViewModel)
                    _navigationService.Navigate(_dynamicViewLocator.RedditView, new SelectSubredditMessage { Subreddit = ((AboutSubredditViewModel)value).Thing });
            }
        }
    }
}
