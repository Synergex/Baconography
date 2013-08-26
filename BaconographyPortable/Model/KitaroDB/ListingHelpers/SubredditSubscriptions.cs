using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.KitaroDB.ListingHelpers
{
    class SubredditSubscriptions : IListingProvider
    {
        IOfflineService _offlineService;
        IUserService _userService;
        public SubredditSubscriptions(IBaconProvider baconProvider)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
            _userService = baconProvider.GetService<IUserService>();
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            var orderedThings = await _offlineService.RetrieveOrderedThings("sublist:" + (await _userService.GetUser()).Username, TimeSpan.FromDays(1024));
            return new Listing { Data = new ListingData { Children = orderedThings != null ? new List<Thing>(orderedThings) : new List<Thing>() } };
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
