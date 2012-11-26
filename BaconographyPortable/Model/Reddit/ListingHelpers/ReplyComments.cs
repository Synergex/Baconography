using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    class ReplyComments : IListingProvider
    {
        IEnumerable<Thing> _things;
        IRedditService _redditService;
        string _contentId;
        string _subreddit;

        public ReplyComments(IBaconProvider baconProvider, IEnumerable<Thing> things, string contentId, string subreddit)
        {
            _redditService = baconProvider.GetService<IRedditService>();
            _things = things;
            _contentId = contentId;
            _subreddit = subreddit;
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return new Listing { Kind = "Listing", Data = new ListingData { Children = _things.ToList() } };
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            return _redditService.GetMoreOnListing(ids, _contentId, _subreddit);
        }
    }
}
