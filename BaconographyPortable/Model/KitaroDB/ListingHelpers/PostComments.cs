using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.KitaroDB.ListingHelpers
{
    class PostComments : IListingProvider
    {
        string _subreddit;
        string _permaLink;
        string _targetName;
        IOfflineService _offlineService;
        ISettingsService _settingsService;

        public PostComments(IBaconProvider baconProvider, string subreddit, string permaLink, string targetName)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
            _settingsService = baconProvider.GetService<ISettingsService>();
            _subreddit = subreddit;
            _permaLink = permaLink;
            _targetName = targetName;
        }

        public Tuple<Task<Listing>, Task<Listing>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Task<Listing>>(null, _offlineService.GetTopLevelComments(_subreddit, _targetName, _settingsService.MaxTopLevelOfflineComments));
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            return _offlineService.GetMoreComments(_subreddit, _targetName, ids);
        }
    }
}
