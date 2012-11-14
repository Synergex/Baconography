using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    class SearchResultsViewModelCollection : ThingViewModelCollection
    {
        string _query;
        public SearchResultsViewModelCollection(IBaconProvider baconProvider, string query) :
            base(baconProvider)
        {
            _query = query;
        }

        protected override Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return _redditService.Search(_query, null);
        }

        protected override Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            //TODO: this url is bleeding over from the model, it should be routed from somewhere on the model side instead
            return _redditService.GetAdditionalFromListing(string.Format("http://www.reddit.com/search.json?q={0}", _query), after, null);
        }
    }
}
