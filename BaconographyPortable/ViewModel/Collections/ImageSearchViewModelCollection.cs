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
    public class ImageSearchViewModelCollection : BaseIncrementalLoadCollection<ImageViewModel>
    {
        IBaconProvider _baconProvider;
        IRedditService _redditService;
        IImagesService _imagesService;
        ISettingsService _settingsService;
        IListingProvider _onlineListingProvider;
        IListingProvider _offlineListingProvider;

        public ImageSearchViewModelCollection(IBaconProvider baconProvider, string query)
        {
            _baconProvider = baconProvider;
            _redditService = baconProvider.GetService<IRedditService>();
            _imagesService = baconProvider.GetService<IImagesService>();
            _settingsService = baconProvider.GetService<ISettingsService>();

            //we only want image results and this seems to be the best way to get that
            var searchQuery = query + " AND (site:'imgur' OR site:'flickr' OR site:'memecrunch' OR site:'quickmeme' OR site:qkme OR site:'min' OR site:'picsarus')";

            _onlineListingProvider = new BaconographyPortable.Model.Reddit.ListingHelpers.SearchResults(_baconProvider, searchQuery);
            _offlineListingProvider = new BaconographyPortable.Model.KitaroDB.ListingHelpers.SearchResults(_baconProvider, searchQuery);
        }

        protected override async Task<IEnumerable<ImageViewModel>> InitialLoad(Dictionary<object, object> state)
        {
            return await MapListing(await GetInitialListing(state), state);
        }

        protected async override Task<IEnumerable<ImageViewModel>> LoadAdditional(Dictionary<object, object> state)
        {
            var after = state["After"] as string;
            state.Remove("After");

            return await MapListing(await GetAdditionalListing(after, state), state);
        }

        private async Task<IEnumerable<ImageViewModel>> MapListing(Listing listing, Dictionary<object, object> state)
        {
            if (listing.Data.After != null)
            {
                state["After"] = listing.Data.After;
            }

            var mappedImages = listing.Data.Children
                .Where(thing => thing.Data is Link)
                .Select(async (thing) => MapImage(await _imagesService.GetImagesFromUrl(((Link)thing.Data).Title, ((Link)thing.Data).Url)));

            var result = new List<ImageViewModel>();
            foreach (var mappedImage in mappedImages)
            {
                result.AddRange(await mappedImage);
            }
            return result;
        }

        private IEnumerable<ImageViewModel> MapImage(IEnumerable<Tuple<string, string>> images)
        {
            return images.Select(tpl => new ImageViewModel { Description = tpl.Item1, Image = tpl.Item2 });
        }

        protected override bool HasAdditional(Dictionary<object, object> state)
        {
            return state.ContainsKey("After") && state["After"] is string;
        }


        private Task<Listing> GetInitialListing(Dictionary<object, object> state)
        {
            if (_settingsService.IsOnline())
                return _onlineListingProvider.GetInitialListing(state).Item2();
            else
                return _offlineListingProvider.GetInitialListing(state).Item2();
        }

        private Task<Listing> GetAdditionalListing(string after, Dictionary<object, object> state)
        {
            if (_settingsService.IsOnline())
                return _onlineListingProvider.GetAdditionalListing(after, state);
            else
                return _offlineListingProvider.GetAdditionalListing(after, state);
        }

        protected override async Task Refresh(Dictionary<object, object> state)
        {
            //TODO implement this
        }
    }
}
