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
        bool _reddits;
        public SearchResults(IBaconProvider baconProvider, string query, bool reddits)
        {
            _query = query;
            _reddits = reddits;
            _redditService = baconProvider.GetService<IRedditService>();
        }

        public Tuple<Task<Listing>, Func<Task<Listing>>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Func<Task<Listing>>>(null, () => _redditService.Search(_query, 20, _reddits));
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            if(_reddits)
                return _redditService.GetAdditionalFromListing(string.Format("http://www.reddit.com/subreddits/search.json?q={0}", _query), after, null);
            else
                return _redditService.GetAdditionalFromListing(string.Format("http://www.reddit.com/search.json?q={0}", _query), after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }


        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return _redditService.Search(_query, 20, _reddits);
        }
    }
}
