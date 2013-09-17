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
            var messages = await _offlineService.GetMessages(await _userService.GetUser());
            //we dont want to toast stale messages so mark them as read
            if (messages != null && messages.Data != null && messages.Data.Children != null)
            {
                foreach (var message in messages.Data.Children)
                {
                    if (message.Data is Message)
                    {
                        ((Message)message.Data).New = false;
                    }
                }
            }
            return messages;

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
