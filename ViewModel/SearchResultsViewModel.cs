using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Actions;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using Baconography.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Baconography.ViewModel
{
    public class SearchResultsViewModel : ViewModelBase
    {
        private IRedditActionQueue _actionQueue;
        private INavigationService _navigationService;
        private IUsersService _userService;

        public SearchResultsViewModel(IRedditActionQueue actionQueue, INavigationService navigationService, IUsersService userService)
        {
            _actionQueue = actionQueue;
            _navigationService = navigationService;
            _userService = userService;

            MessengerInstance.Register<SearchQueryMessage>(this, (queryMessage) =>
                {
                    Query = queryMessage.Query;
                    Results = new ThingViewModelCollection
                    {
                        ActionQueue = _actionQueue,
                        TargetListing = new Listing { Data = new ListingData { Children = new List<Thing>() } },
                        BaseListingUrl = Search.MakeSearchUrl(Query),
                        UserService = _userService,
                        NavigationService = _navigationService
                    };
                });

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
                else if (value is SubredditViewModel)
                    _navigationService.Navigate<RedditView>(new SelectSubreddit { Subreddit = ((SubredditViewModel)value).Thing });
            }
        }
    }
}
