using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.KitaroDB.ListingHelpers
{
    class SubredditInfo : IListingProvider, IDontRefreshAutomatically
    {
        IOfflineService _offlineService;
        IUserService _userService;
        public SubredditInfo(IBaconProvider baconProvider)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
            _userService = baconProvider.GetService<IUserService>();
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            var orderedThings = await _offlineService.RetrieveOrderedThings("sublist:" + (await _userService.GetUser()).Username, TimeSpan.FromDays(1024));
            if (orderedThings == null)
                return new Listing { Data = new ListingData { Children = new List<Thing>() } };

            state["SubscribedSubreddits"] = ThingUtility.HashifyListing(orderedThings);
            var things = await _offlineService.RetrieveOrderedThings("reddits:", TimeSpan.FromDays(1024));
            if (things == null || things.Count() == 0)
                return new Listing { Data = new ListingData { Children = new List<Thing>() } };
            return new Listing { Data = new ListingData { Children = new List<Thing>(things) } };
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
