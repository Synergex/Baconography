using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.KitaroDB.ListingHelpers
{
    class PostMessages : IListingProvider
    {
        IOfflineService _offlineService;
        ISettingsService _settingsService;
        IUserService _userService;

        public PostMessages(IBaconProvider baconProvider)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
            _settingsService = baconProvider.GetService<ISettingsService>();
            _userService = baconProvider.GetService<IUserService>();
        }

        public Tuple<Task<Listing>, Func<Task<Listing>>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Func<Task<Listing>>>(null, () => _offlineService.GetMessages(_userService.GetUser().Result));
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }
        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return GetInitialListing(state).Item2();
        }
    }
}
