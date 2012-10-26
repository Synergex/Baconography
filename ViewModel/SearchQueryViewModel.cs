using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Baconography.Messages;
using Baconography.RedditAPI.Actions;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.ViewModel
{
    public class SearchQueryViewModel : ViewModelBase
    {
        private INavigationService _nav;

        public SearchQueryViewModel( INavigationService nav )
        {
            _nav = nav;
        }

        private string _query;
        public string Query
        {
            get { return _query; }
            set
            {
                _query = value;
                RaisePropertyChanged( "Query" );
            }
        }

        RelayCommand _search;
        public RelayCommand Search
        {
            get
            {
                if ( _search == null )
                {
                    _search = new RelayCommand( () =>
                    {
                        _nav.Navigate<Baconography.View.SearchResultsView>( Query );
                        Query = null;
                    });
                }
                return _search;
            }
        }
    }
}
