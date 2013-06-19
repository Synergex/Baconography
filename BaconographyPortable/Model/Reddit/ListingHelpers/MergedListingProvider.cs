using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit.ListingHelpers
{
    public class MergedListingProvider<T> : IListingProvider
        where T : class,IThingData
    {
        IEnumerable<IListingProvider> _providers;
        IListingProvider _initialProvider;

        public MergedListingProvider(IListingProvider initalProvider, IEnumerable<IListingProvider> additionalProviders, Func<T, string> selectIdFromT)
        {
            _initialProvider = initalProvider;
            _providers = additionalProviders;
            _selectIdFromT = selectIdFromT;
        }

        Func<T, string> _selectIdFromT;

        string CallUserSelect(Thing p)
        {
            var temp = new TypedThing<T>(p);
            return _selectIdFromT(temp.TypedData);
        }

        public Tuple<Task<Listing>, Func<Task<Listing>>> GetInitialListing(Dictionary<object, object> state)
        {
            return Tuple.Create<Task<Listing>, Func<Task<Listing>>>(null, async () =>
            {
                Listing listing = await _initialProvider.GetInitialListing(state).Item2();
                IEnumerable<string> ids = listing.Data.Children.Select(CallUserSelect);
                foreach (var provider in _providers)
                {
                    if (ids.Count() == 0)
                    {
                        listing = await provider.GetInitialListing(state).Item2();
                        ids = listing.Data.Children.Select(CallUserSelect);
                    }
                    else
                    {
                        var temp = await provider.GetMore(ids, state);
                        if (temp.Data.Children != null && temp.Data.Children.Count > 0)
                        {
                            ids = ids.Concat(temp.Data.Children.Select(CallUserSelect)).Distinct();
                            listing.Data.Children = listing.Data.Children.Concat(temp.Data.Children).ToList();
                        }
                    }
                }

                return listing;
            });
        }

        public Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        public async Task<Listing> GetMore(IEnumerable<string> ids, Dictionary<object, object> state)
        {
            Listing listing = await _initialProvider.GetMore(ids, state);
            ids = ids.Concat(listing.Data.Children.Select(CallUserSelect));
            foreach (var provider in _providers)
            {
                var temp = await provider.GetMore(ids, state);
                if (temp.Data.Children != null && temp.Data.Children.Count > 0)
                {
                    ids = ids.Concat(temp.Data.Children.Select(CallUserSelect)).Distinct();
                    listing.Data.Children = listing.Data.Children.Concat(temp.Data.Children).ToList();
                }
            }

            return listing;
        }

        public Task<Listing> Refresh(Dictionary<object, object> state)
        {
            return GetInitialListing(state).Item2();
        }
    }
}
