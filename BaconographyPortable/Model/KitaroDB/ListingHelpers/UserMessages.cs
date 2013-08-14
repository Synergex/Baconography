using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.KitaroDB.ListingHelpers
{
    class UserMessages : IListingProvider
    {
        IOfflineService _offlineService;
        ISettingsService _settingsService;
        IUserService _userService;

        public UserMessages(IBaconProvider baconProvider)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
            _settingsService = baconProvider.GetService<ISettingsService>();
            _userService = baconProvider.GetService<IUserService>();
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return await _offlineService.GetMessages(await _userService.GetUser());
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
            return GetInitialListing(state);
        }
    }
}
