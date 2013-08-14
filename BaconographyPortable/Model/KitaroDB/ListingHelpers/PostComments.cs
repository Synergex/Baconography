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

        public Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return _offlineService.GetTopLevelComments(_permaLink, 500);
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            return _offlineService.GetMoreComments(_subreddit, _targetName, ids);
        }
        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return GetInitialListing(state);
        }
    }
}
