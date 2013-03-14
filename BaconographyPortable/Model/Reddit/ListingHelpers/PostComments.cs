using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class PostComments : IListingProvider
    {
        IRedditService _redditService;
        string _subreddit;
        string _permaLink;
        string _targetName;

        public PostComments(IBaconProvider baconProvider, string subreddit, string permaLink, string targetName)
        {
            _redditService = baconProvider.GetService<IRedditService>();
            _subreddit = subreddit;
            _permaLink = permaLink;
            _targetName = targetName;
        }

        public Tuple<Task<Listing>, Task<Listing>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Task<Listing>>(null, _redditService.GetCommentsOnPost(_subreddit, _permaLink, -1));
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://reddit.com" + _permaLink, after, null);
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            return _redditService.GetMoreOnListing(ids, _targetName, _subreddit);
        }
    }
}
