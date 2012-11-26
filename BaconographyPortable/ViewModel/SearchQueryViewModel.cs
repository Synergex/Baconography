using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class SearchQueryViewModel : ViewModelBase
    {
        private INavigationService _navigationService;
        private IDynamicViewLocator _viewLocator;

        public SearchQueryViewModel(IBaconProvider baconProvider)
        {
            _navigationService = baconProvider.GetService<INavigationService>();
            _viewLocator = baconProvider.GetService<IDynamicViewLocator>();
        }

        private string _query;
        public string Query
        {
            get { return _query; }
            set
            {
                _query = value;
                RaisePropertyChanged("Query");
            }
        }

        public static RelayCommand<SearchQueryViewModel> Search { get { return _search; } }

        static RelayCommand<SearchQueryViewModel> _search = new RelayCommand<SearchQueryViewModel>((vm) => vm.SearchImpl());
        
        private void SearchImpl()
        {
            _navigationService.Navigate(_viewLocator.SearchResultsView, Query);
            Query = null;
        }
    }
}
