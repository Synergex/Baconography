using BaconographyPortable.Common;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class CommentViewModelCollection : ThingViewModelCollection
    {
        string _permaLink;
        string _subreddit;
        string _targetName;
        public CommentViewModelCollection(IBaconProvider baconProvider, string permaLink, string subreddit, string targetName)
            : base(baconProvider) 
        {
            _permaLink = permaLink;
            _subreddit = subreddit;
            _targetName = targetName;
        }

        protected override Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return _redditService.GetCommentsOnPost(_subreddit, _permaLink, null);
        }

        protected override Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            return _redditService.GetAdditionalFromListing("http://reddit.com" + _permaLink, after, null);
        }

        protected override Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            return _redditService.GetMoreOnListing(ids, _targetName, _subreddit);
        }
    }
}
