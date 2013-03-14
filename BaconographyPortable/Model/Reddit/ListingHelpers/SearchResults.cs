using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class SearchResults : IListingProvider
    {
        IRedditService _redditService;
        string _query;
        public SearchResults(IBaconProvider baconProvider, string query)
        {
            _query = query;
            _redditService = baconProvider.GetService<IRedditService>();
        }

        public Tuple<Task<Listing>, Task<Listing>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Task<Listing>>(null,_redditService.Search(_query, 20));
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing(string.Format("http://www.reddit.com/search.json?q={0}", _query), after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }
    }
}
