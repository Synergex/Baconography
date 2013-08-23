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
        string _restrictedToSubreddit;
        public SearchResults(IBaconProvider baconProvider, string query, bool reddits, string restrictedToSubreddit)
        {
            _query = query;
            _reddits = reddits;
            _restrictedToSubreddit = restrictedToSubreddit;
            _redditService = baconProvider.GetService<IRedditService>();
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            if (string.IsNullOrWhiteSpace(_query))
                return new Listing { Data = new ListingData { Children = new List<Thing>() }, Kind = "Listing" };
            else
                return await _redditService.Search(_query, 20, _reddits, _restrictedToSubreddit);
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            if(_reddits)
                return _redditService.GetAdditionalFromListing(string.Format("http://www.reddit.com/subreddits/search.json?q={0}", _query), after, null);
            else if(string.IsNullOrWhiteSpace(_restrictedToSubreddit))
                return _redditService.GetAdditionalFromListing(string.Format("http://www.reddit.com/search.json?q={0}", _query), after, null);
            else
                return _redditService.GetAdditionalFromListing(string.Format("http://www.reddit.com/r/{1}/search.json?q={0}", _query, _restrictedToSubreddit), after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }


        public async Task<Listing> Refresh(Dictionary<object, object> state)
        {
            if (string.IsNullOrWhiteSpace(_query))
                return new Listing { Data = new ListingData { Children = new List<Thing>() }, Kind = "Listing" };
            else
                return await _redditService.Search(_query, 20, _reddits, _restrictedToSubreddit);
        }
    }
}
