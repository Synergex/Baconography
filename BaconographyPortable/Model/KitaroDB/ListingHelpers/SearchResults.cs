﻿using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.KitaroDB.ListingHelpers
{
    class SearchResults : IListingProvider
    {
        IOfflineService _offlineService;
        string _query;

        public SearchResults(IBaconProvider baconProvider, string query)
        {
            _offlineService = baconProvider.GetService<IOfflineService>();
            _query = query;
        }

        public async Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            return new Listing { Kind = "Listing", Data = new ListingData { Children = new List<Thing>() } };
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }
    }
}
