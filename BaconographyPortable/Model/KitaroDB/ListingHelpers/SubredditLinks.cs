using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.KitaroDB.ListingHelpers
{
    class SubredditLinks : IListingProvider
    {
        private string _subreddit;
        private IOfflineService _offlineService;

        public SubredditLinks(IBaconProvider baconProvider, string subreddit, string subredditId)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
            _subreddit = subreddit;
        }

        public Tuple<Task<Listing>, Task<Listing>> GetInitialListing(Dictionary<object, object> state)
        {
            Task<Listing> result = null;
            if (_subreddit != null && _subreddit != "/")
                result = _offlineService.LinksForSubreddit(_subreddit, null);
            else
                result = _offlineService.AllLinks(null);

            return Tuple.Create<Task<Listing>, Task<Listing>>(null, result);
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            if (_subreddit != null && _subreddit != "/")
                return _offlineService.LinksForSubreddit(_subreddit, after);
            else
            {
                return _offlineService.AllLinks(after);
            }
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }
    }
}
