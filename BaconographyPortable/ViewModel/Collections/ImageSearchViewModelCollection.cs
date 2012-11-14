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
        private string _query;
        IRedditService _redditService;
        IImagesService _imagesService;

        public ImageSearchViewModelCollection(IRedditService redditService, IImagesService imagesService, string query)
        {
            _redditService = redditService;
            _imagesService = imagesService;
            _query = query;
        }

        protected override async Task<IEnumerable<ImageViewModel>> InitialLoad(Dictionary<object, object> state)
        {
            var searchQuery = _query + " AND (site:'imgur' OR site:'flickr' OR site:'memecrunch' OR site:'quickmeme' OR site:qkme OR site:'min' OR site:'picsarus')";
            var searchResults = await _redditService.Search(searchQuery, null);

            return await MapListing(searchResults, state);
        }

        protected async override Task<IEnumerable<ImageViewModel>> LoadAdditional(Dictionary<object, object> state)
        {
            var after = state["After"] as string;
            state.Remove("After");

            return await MapListing(await _redditService.GetAdditionalFromListing("http://www.reddit.com/reddits", after, null), state);
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
            return state.ContainsKey("After");
        }
    }
}
