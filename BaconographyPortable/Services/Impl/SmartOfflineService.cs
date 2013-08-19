using BaconographyPortable.Model.Reddit;
using BaconographyPortable.ViewModel;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaconographyPortable.Services.Impl
{
    public class SmartOfflineService : ISmartOfflineService
    {
        IViewModelContextService _viewModelContextService;
        IOOMService _oomService;
        ISettingsService _settingsService;
        ISuspensionService _suspensionService;
        IDynamicViewLocator _dynamicViewLocator;
        IOfflineService _offlineService;
        IImagesService _imagesService;
        ISystemServices _systemServices;
        ISuspendableWorkQueue _suspendableWorkQueue;
        RedditViewModel _firstRedditViewModel;
        CommentsViewModel _firstCommentsViewModel;
        CancellationTokenSource _cancelationTokenSource = new CancellationTokenSource();

        public void Initialize(IViewModelContextService viewModelContextService, IOOMService oomService, ISettingsService settingsService, 
            ISuspensionService suspensionService, IDynamicViewLocator dynamicViewLocator, IOfflineService offlineService, IImagesService imagesService,
            ISystemServices systemServices, ISuspendableWorkQueue suspendableWorkQueue)
        {
            _viewModelContextService = viewModelContextService;
            _oomService = oomService;
            _settingsService = settingsService;
            _suspensionService = suspensionService;
            _dynamicViewLocator = dynamicViewLocator;
            _offlineService = offlineService;
            _imagesService = imagesService;
            _systemServices = systemServices;
            _suspendableWorkQueue = suspendableWorkQueue;

            _oomService.OutOfMemory += _oomService_OutOfMemory;
        }

        void _oomService_OutOfMemory(OutOfMemoryEventArgs obj)
        {
            _cancelationTokenSource.Cancel();
        }

        public void ClearOfflineData()
        {
            //TODO: this
        }

        public async void NavigatedToOfflineableThing(Model.Reddit.Thing targetThing, bool link)
        {
            await NavigatedToOfflineableThingImpl(targetThing, link);
        }

        private async Task NavigatedToOfflineableThingImpl(Model.Reddit.Thing targetThing, bool link)
        {
            //if we've offlined this thing, we need to pat ourselves on the back
            //because we got it right

            if (targetThing != null && targetThing.Data is Link)
            {
                var targetLink = targetThing.Data as Link;
                try
                {
                    if(!string.IsNullOrWhiteSpace(targetLink.Domain))
                        await _offlineService.IncrementDomainStatistic(targetLink.Domain, link);
                    if (!string.IsNullOrWhiteSpace(targetLink.SubredditId))
                        await _offlineService.IncrementSubredditStatistic(targetLink.SubredditId, link);
                }
                catch
                {
                    //dont care what the error is, this isnt an acceptable place to fail or present the user with a failure
                }
            }
        }

        int _navId = 0;
        DateTime lastOppertunity = DateTime.Now;
        public async void NavigatedToView(Type viewType, bool forward)
        {
            _navId++;
            int myNavID = _navId;

            _cancelationTokenSource.Cancel();

            var currentContext = _viewModelContextService.Context;
            if (currentContext is CommentsViewModel && ((CommentsViewModel)currentContext).Link != null)
            {
                try
                {
                    await _suspendableWorkQueue.QueueLowImportanceRestartableWork(async (token) =>
                        {
                            await NavigatedToOfflineableThingImpl(((CommentsViewModel)currentContext).Link.LinkThing, true);
                        });
                }
                catch (TaskCanceledException)
                {
                }
            }

            if (myNavID != _navId)
            {
                _cancelationTokenSource.Cancel();
                return;
            }
            else
                _cancelationTokenSource = new CancellationTokenSource();


            if ((DateTime.Now - lastOppertunity).TotalSeconds < 10)
                return;
            else
                lastOppertunity = DateTime.Now;

            //determine if this is a good time to be caching things
            //and what should be our highest priority to be cached
            var viewModelContextStack = _viewModelContextService.ContextStack;
            _firstRedditViewModel = viewModelContextStack.OfType<RedditViewModel>().FirstOrDefault();
            _firstCommentsViewModel = viewModelContextStack.OfType<CommentsViewModel>().FirstOrDefault();

            var networkAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

            //no network means there is no reason for us to do anything
            if (!networkAvailable)
                return;
            var onMeteredConnection = _systemServices.IsOnMeteredConnection;

            OffliningOpportunityPriority priority = OffliningOpportunityPriority.None;
            if (viewType == _dynamicViewLocator.LinkedPictureView && forward)
                priority = OffliningOpportunityPriority.Image;

            else if (viewType == _dynamicViewLocator.CommentsView && forward)
                priority = OffliningOpportunityPriority.Comments;

            else if (viewType == _dynamicViewLocator.RedditView)
                priority = OffliningOpportunityPriority.ImageAPI;

            OffliningOpportunity(priority, onMeteredConnection ? NetworkConnectivityStatus.Metered : NetworkConnectivityStatus.Unmetered, _cancelationTokenSource.Token);
        }

        public IEnumerable<string> OfflineableImagesFromContext
        {
            get 
            {
                var imagesViewModel = _viewModelContextService.ContextStack.OfType<LinkedPictureViewModel>().FirstOrDefault();
                if (imagesViewModel != null)
                {
                    return imagesViewModel.Pictures.Select(vm => vm.Url);
                }
                else if (_firstRedditViewModel != null)
                {
                    //this is less usefull than it initially appears
                    //top visible link is only updated when you leave the subreddit in the pivot
                    //leading to somewhat stale data
                    var loadedLinks = _firstRedditViewModel.Links;
                    IEnumerable<ViewModelBase> importantLinks = loadedLinks;
                    if (_firstRedditViewModel.TopVisibleLink != null)
                        importantLinks = loadedLinks.SkipWhile(vm => vm != _firstRedditViewModel.TopVisibleLink);

                    return importantLinks.OfType<LinkViewModel>().Where(vm => _imagesService.IsImage(vm.Url)).Select(vm => vm.Url);
                }
                else
                    return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> OfflineableLinksFromContext
        {
            get 
            {
                if (_firstRedditViewModel != null && !IsActivityIdle)
                {
                    //this is less usefull than it initially appears
                    //top visible link is only updated when you leave the subreddit in the pivot
                    //leading to somewhat stale data
                    var loadedLinks = _firstRedditViewModel.Links;
                    IEnumerable<ViewModelBase> importantLinks = loadedLinks;
                    if (_firstRedditViewModel.TopVisibleLink != null)
                        importantLinks = loadedLinks.SkipWhile(vm => vm != _firstRedditViewModel.TopVisibleLink);

                    return importantLinks.OfType<LinkViewModel>().Where(vm => !_imagesService.IsImage(vm.Url) && !_imagesService.IsImageAPI(vm.Url)).Select(vm => vm.Url);
                }
                else
                    return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<TypedThing<Link>> OfflineableLinkThingsFromContext
        {
            get 
            {
                if (_firstRedditViewModel != null && !IsActivityIdle)
                {
                    //this is less usefull than it initially appears
                    //top visible link is only updated when you leave the subreddit in the pivot
                    //leading to somewhat stale data
                    var loadedLinks = _firstRedditViewModel.Links;
                    IEnumerable<ViewModelBase> importantLinks = loadedLinks;
                    if (_firstRedditViewModel.TopVisibleLink != null)
                        importantLinks = loadedLinks.SkipWhile(vm => vm != _firstRedditViewModel.TopVisibleLink);

                    return importantLinks.OfType<LinkViewModel>().Select(vm => vm.LinkThing);
                }
                else
                    return Enumerable.Empty<TypedThing<Link>>();
            }
        }

        public IEnumerable<string> OfflineableImageAPIsFromContext
        {
            get 
            { 
                //we're looking for two possible contexts here, RedditViewModel and CommentsViewModel
                //need to look through the link things for offlineable image api's, not entirely sure what priority CommentsView stuff should be
                //given that it might be expensive to compute

                if (_firstRedditViewModel != null && !IsActivityIdle)
                {
                    //this is less usefull than it initially appears
                    //top visible link is only updated when you leave the subreddit in the pivot
                    //leading to somewhat stale data
                    var loadedLinks = _firstRedditViewModel.Links;
                    IEnumerable<ViewModelBase> importantLinks = loadedLinks;
                    if (_firstRedditViewModel.TopVisibleLink != null)
                        importantLinks = loadedLinks.SkipWhile(vm => vm != _firstRedditViewModel.TopVisibleLink);
                    
                    return importantLinks.OfType<LinkViewModel>().Where(vm => _imagesService.IsImageAPI(vm.Url)).Select(vm => vm.Url);
                }
                else
                    return Enumerable.Empty<string>();
            }
        }

        public bool IsActivityIdle { get; set; }

        public event Action<OffliningOpportunityPriority, NetworkConnectivityStatus, CancellationToken> OffliningOpportunity;

    }
}
