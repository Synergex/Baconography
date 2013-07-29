using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services.Impl
{
    public class SmartOfflineImageService : IImagesService
    {
        IImagesService _imagesService;
        IOfflineService _offlineService;
        IOOMService _oomService;
        ISuspensionService _suspensionService;
        ISmartOfflineService _smartOfflineService;
        ISettingsService _settingsService;

        public void Initialize(IImagesService imagesService, IOfflineService offlineService, IOOMService oomService, ISettingsService settingsService,
            ISuspensionService suspensionService, ISmartOfflineService smartOfflineService, ISimpleHttpService simpleHttpService)
        {
            _imagesService = imagesService;
            _offlineService = offlineService;
            _oomService = oomService;
            _settingsService = settingsService;
            _suspensionService = suspensionService;
            _smartOfflineService = smartOfflineService;

            _smartOfflineService.OffliningOpportunity += _smartOfflineService_OffliningOpportunity;
            _oomService.OutOfMemory += _oomService_OutOfMemory;
        }

        void _oomService_OutOfMemory(OutOfMemoryEventArgs obj)
        {
            _activeImages = null;
            _urlsOfflined.Clear();
        }

        bool _inflightOfflining = false;

        Dictionary<string, WeakReference<byte[]>> _activeImages = new Dictionary<string, WeakReference<byte[]>>();

        private Dictionary<string, WeakReference<byte[]>> ActiveImages
        {
            get
            {
                lock (this)
                {
                    var activeImages = _activeImages;
                    if (activeImages == null)
                        _activeImages = activeImages = new Dictionary<string, WeakReference<byte[]>>();

                    return activeImages;
                }
            }
        }

        Stack<string> _waitingOfflineImages = new Stack<string>();
        Stack<string> _waitingOfflineAPI = new Stack<string>();
        HashSet<string> _urlsOfflined = new HashSet<string>();

        async void _smartOfflineService_OffliningOpportunity(OffliningOpportunityPriority priority, NetworkConnectivityStatus networkStatus, System.Threading.CancellationToken token)
        {
            if (_inflightOfflining)
                return;

            _inflightOfflining = true;
            try
            {
                if (!_smartOfflineService.IsActivityIdle && priority == OffliningOpportunityPriority.Image)
                {
                    //we're probably looking at images right now, possibly in a gallery so grab as much as possible
                    //metered or not doesnt matter here since they were going to download the image anyway
                    foreach (var offlinableImage in _smartOfflineService.OfflineableImagesFromContext)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        await ImageBytesFromUrl(offlinableImage);
                    }
                    return;
                }


                //if we're on unmetered we can be pretty agressive about our downloading
                //otherwise just download the thumbnails and make the api calls since the platform default behavior would have been to download them anyway
                if (networkStatus == NetworkConnectivityStatus.Unmetered)
                {
                    if (token.IsCancellationRequested)
                        return;

                    string targetImageToOffline = null;
                    lock (_waitingOfflineImages)
                    {
                        if (_waitingOfflineImages.Count == 0)
                            foreach (var item in _smartOfflineService.OfflineableImagesFromContext.Reverse())
                                _waitingOfflineImages.Push(item);

                        if(_waitingOfflineImages.Count > 0)
                            targetImageToOffline = _waitingOfflineImages.Pop();
                    }

                    if(targetImageToOffline != null)
                        await ImageBytesFromUrl(targetImageToOffline);
                }

                if (token.IsCancellationRequested)
                    return;

                string targetAPIToOffline = null;
                do
                {
                    lock (_waitingOfflineAPI)
                    {
                        if (_waitingOfflineAPI.Count == 0 && targetAPIToOffline == null)
                            foreach (var item in _smartOfflineService.OfflineableImageAPIsFromContext.Reverse())
                                _waitingOfflineAPI.Push(item);

                        if (_waitingOfflineAPI.Count > 0)
                            targetAPIToOffline = _waitingOfflineAPI.Pop();
                        else
                            targetAPIToOffline = null;
                    }

                    if (targetAPIToOffline != null)
                    {
                        if (_urlsOfflined.Contains(targetAPIToOffline))
                            continue;
                        else
                            _urlsOfflined.Add(targetAPIToOffline);

                        await GetImagesFromUrl("", targetAPIToOffline);
                    }

                    if (token.IsCancellationRequested)
                        return;

                } while (targetAPIToOffline != null);
            }
            catch (Exception ex)
            {
            }
            finally
            {
                _inflightOfflining = false;
            }
        }

        public async Task<IEnumerable<Tuple<string, string>>> GetImagesFromUrl(string title, string url)
        {
            if (IsImageAPI(url))
            {
                var cachedResult = await _offlineService.GetImages(url);
                if (cachedResult != null)
                    return cachedResult;
                else
                {
                    var results = await _imagesService.GetImagesFromUrl(title, url);
                    await _offlineService.StoreImages(results, url);
                    return results;
                }
            }
            else if (IsImage(url))
            {
                return new Tuple<string, string>[] { new Tuple<string, string>(title, url) };
            }
            else
                return null;
        }

        public bool MightHaveImagesFromUrl(string url)
        {
            return _imagesService.MightHaveImagesFromUrl(url);
        }

        public bool IsImage(string url)
        {
            return _imagesService.IsImage(url);
        }

        public bool IsImageAPI(string url)
        {
            return _imagesService.IsImageAPI(url);
        }

        public Task<object> GenerateResizedImage(object inputFile, uint width, uint height, uint edgePadding = 5, uint bottomPadding = 20, bool replaceIfExists = true)
        {
            return _imagesService.GenerateResizedImage(inputFile, width, height, edgePadding, bottomPadding, replaceIfExists);
        }

        public Task<object> SaveFileFromUriAsync(Uri fileUri, string localFileName, string localPath = "Images", bool replaceIfExists = true)
        {
            return _imagesService.SaveFileFromUriAsync(fileUri, localFileName, localPath, replaceIfExists);
        }

        public Task<byte[]> ImageBytesFromUrl(string url)
        {
            //there doesnt seem to be any reason to cache image bytes, the os is doing a fine job of it at least on WP8
            return _imagesService.ImageBytesFromUrl(url);
        }
    }
}
