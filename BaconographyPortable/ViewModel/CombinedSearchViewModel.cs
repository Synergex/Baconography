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
    public class CombinedSearchViewModel : ViewModelBase
    {
        ISystemServices _systemServices;
        IBaconProvider _baconProvider;
        IViewModelContextService _viewModelContext;
        public CombinedSearchViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _viewModelContext = baconProvider.GetService<IViewModelContextService>();
            _systemServices = baconProvider.GetService<ISystemServices>();
            SearchResults = new BindingShellViewModelCollection(new SearchResultsViewModelCollection(_baconProvider, "", false));
            
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
                bool wasChanged = _query != value;
                if (wasChanged)
                {
                    _query = value;
                    RaisePropertyChanged("Query");

                    if (_query.Length < 3)
                    {
                        SearchResults.RevertToDefault();
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
            if (SearchResults != null)
            {
                if (!string.IsNullOrWhiteSpace(_query))
                    SearchResults.UpdateRealItems(new SearchResultsViewModelCollection(_baconProvider, _query, false, SearchOnlySubreddit ? TargetSubreddit : null));
            }
        }

        private string _targetSubreddit;
        public string TargetSubreddit
        {
            get 
            {
                var currentRedditVM = _viewModelContext.ContextStack.FirstOrDefault(vm => vm is RedditViewModel) as RedditViewModel;
                if (currentRedditVM != null)
                {
                    _targetSubreddit = currentRedditVM.Heading;
                    if (_targetSubreddit == "The front page of this device")
                        _targetSubreddit = null;
                }
                return _targetSubreddit; 
            }
            set
            {
                _targetSubreddit = value;
                RaisePropertyChanged("TargetSubreddit");
            }
        }

        private bool _searchOnlySubreddit;
        public bool SearchOnlySubreddit
        {
            get { return _searchOnlySubreddit; }
            set
            {
                _searchOnlySubreddit = value;
                RaisePropertyChanged("SearchOnlySubreddit");
                if (_query != null && _query.Length < 3)
                {
                    SearchResults.RevertToDefault();
                    RevokeQueryTimer();
                }
                else
                {
                    RestartQueryTimer();
                }
            }
        }


        private BindingShellViewModelCollection _searchResults;
        public BindingShellViewModelCollection SearchResults
        {
            get { return _searchResults; }
            set
            {
                _searchResults = value;
                RaisePropertyChanged("SearchResults");
            }
        }
    }
}
