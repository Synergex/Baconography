using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyW8.PlatformServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace BaconographyW8BackgroundTask
{
    public sealed class LiveTileUpdater : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        IImagesService _imagesService;
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            var baconProvider = new BaconProvider(new Tuple<Type, Object>[] { new Tuple<Type, Object>(typeof(IDynamicViewLocator), new DummyViewLocator()) });
            await baconProvider.Initialize(null);

            await RunBodyImpl(baconProvider);
            
            _deferral.Complete();
        }

        public async void RunBody(object baconProviderObj)
        {
            IBaconProvider baconProvider = baconProviderObj as IBaconProvider;
            await RunBodyImpl(baconProvider);
        }

        private async Task RunBodyImpl(IBaconProvider baconProvider)
        {
            try
            {
                DateTime start = DateTime.Now;
                DateTime killDate = start.Subtract(new TimeSpan(1, 0, 0, 0));
                var posts = await baconProvider.GetService<IRedditService>().GetPostsBySubreddit("/", 10);

                if (baconProvider.GetService<ISettingsService>().IsOnline())
                {

                    var liveTileService = baconProvider.GetService<ILiveTileService>();
                    _imagesService = baconProvider.GetService<IImagesService>();
                    //baconProvider.GetService<ISettingsService>().PreferImageLinksForTiles;
                    var linkComparer = new LinkComparer(true);

                    SortedSet<Tuple<string, string, TypedThing<Link>>> sortedLinks = new SortedSet<Tuple<string, string, TypedThing<Link>>>(linkComparer);

                    foreach (var link in posts.Data.Children.Where(thing => thing.Data is Link))
                        sortedLinks.Add(await MapLink(link));

                    foreach (var linkTpl in sortedLinks)
                    {
                        try
                        {
                            await liveTileService.MaybeCreateTile(linkTpl);
                        }
                        catch
                        {
                        }
                    }
                    var liveTilesFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync("liveTiles");
                    if (liveTilesFolder != null)
                    {
                        foreach (var file in await liveTilesFolder.GetFilesAsync())
                        {
                            if (file.DateCreated.LocalDateTime < killDate)
                            {
                                await file.DeleteAsync();
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        //get the thumbnail url, the processed image url and the typed link thing
        private async Task<Tuple<string, string, TypedThing<Link>>> MapLink(Thing link)
        {
            var linkThing = new TypedThing<Link>(link);
            string processedImageUrl = null;
            string thumbnail = null;
            var imagesTpls = await _imagesService.GetImagesFromUrl("", linkThing.Data.Url);
            var imageTpl = imagesTpls.FirstOrDefault();
            if (imageTpl != null)
            {
                processedImageUrl = imageTpl.Item2;
            }

            if (linkThing.Data.Thumbnail != "default" && linkThing.Data.Thumbnail != "nsfw" && linkThing.Data.Thumbnail != "self")
                thumbnail = linkThing.Data.Thumbnail;

            return Tuple.Create(thumbnail, processedImageUrl, linkThing);
        }

        private class LinkComparer : IComparer<Tuple<string, string, TypedThing<Link>>>
        {
            bool _preferImageLinksForTiles;
            public LinkComparer(bool preferImageLinksForTiles)
            {
                _preferImageLinksForTiles = preferImageLinksForTiles;
            }
            public int Compare(Tuple<string, string, TypedThing<Link>> x, Tuple<string, string, TypedThing<Link>> y)
            {
                if (_preferImageLinksForTiles && !string.IsNullOrWhiteSpace(x.Item2) && string.IsNullOrWhiteSpace(y.Item2))
                    return -1;
                else if (!string.IsNullOrWhiteSpace(x.Item1) && string.IsNullOrWhiteSpace(y.Item1))
                    return -1;
                else
                    return 1;

            }
        }
        
    }
}
