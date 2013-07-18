using Baconography.NeutralServices;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.Services.Impl;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BaconographyWP8.PlatformServices
{
    public class BaconProvider : IBaconProvider
    {
        public BaconProvider(IEnumerable<Tuple<Type, Object>> initialServices)
        {
            WebRequest.RegisterPrefix("http://", SharpGIS.WebRequestCreator.GZip);
            WebRequest.RegisterPrefix("https://", SharpGIS.WebRequestCreator.GZip);

            var suspensionService = new SuspensionService();
            var redditService = new RedditService();
            var imagesService = new ImagesService();
            var liveTileService = new LiveTileService();
            var notificationService = new NotificationService();
            var settingsService = new SettingsService();
            var offlineService = new OfflineService(redditService, notificationService, settingsService, suspensionService);
            var simpleHttpService = new SimpleHttpService();
            var systemServices = new SystemServices();
            var navigationService = new NavigationServices();
            var webViewWrapper = new WebViewWrapper();
            var userService = new UserService();
			var videoService = new VideoService(simpleHttpService, notificationService, settingsService);
            var smartImageService = new SmartOfflineImageService();
            var smartRedditService = new SmartOfflineRedditService();
            var oomService = new OOMService();
            var smartOfflineService = new SmartOfflineService();
            var viewModelContextService = new ViewModelContextService();
            


            _services = new Dictionary<Type, object>
            {
                {typeof(IImagesService), imagesService},
                {typeof(ILiveTileService), liveTileService},
                {typeof(IRedditService), smartRedditService},
                {typeof(IOfflineService), offlineService},
                {typeof(ISimpleHttpService), simpleHttpService},
                {typeof(INotificationService), notificationService},
                {typeof(ISettingsService), settingsService},
                {typeof(ISystemServices), systemServices},
                {typeof(INavigationService), navigationService},
                {typeof(IWebViewWrapper), webViewWrapper},
                {typeof(IUserService), userService},
				{typeof(IVideoService), videoService},
                {typeof(IOOMService), oomService},
                {typeof(ISmartOfflineService), smartOfflineService},
                {typeof(ISuspensionService), suspensionService},
                {typeof(IViewModelContextService), viewModelContextService}
            };

            foreach (var initialService in initialServices)
            {
                _services.Add(initialService.Item1, initialService.Item2);
            }


            smartImageService.Initialize(imagesService, offlineService, oomService, settingsService, suspensionService, smartOfflineService, simpleHttpService);
            smartRedditService.Initialize(smartOfflineService, suspensionService, redditService, settingsService, systemServices, offlineService,notificationService, userService);
            smartOfflineService.Initialize(viewModelContextService, oomService, settingsService, suspensionService, _services[typeof(IDynamicViewLocator)] as IDynamicViewLocator, offlineService, imagesService, systemServices);


            SimpleIoc.Default.Register<IImagesService>(() => imagesService);
            SimpleIoc.Default.Register<ILiveTileService>(() => liveTileService);
            SimpleIoc.Default.Register<IRedditService>(() => smartRedditService);
            SimpleIoc.Default.Register<IOfflineService>(() => offlineService);
            SimpleIoc.Default.Register<ISimpleHttpService>(() => simpleHttpService);
            SimpleIoc.Default.Register<INotificationService>(() => notificationService);
            SimpleIoc.Default.Register<ISettingsService>(() => settingsService);
            SimpleIoc.Default.Register<ISystemServices>(() => systemServices);
            SimpleIoc.Default.Register<INavigationService>(() => navigationService);
            SimpleIoc.Default.Register<IWebViewWrapper>(() => webViewWrapper);
            SimpleIoc.Default.Register<IUserService>(() => userService);
			SimpleIoc.Default.Register<IVideoService>(() => videoService);
            SimpleIoc.Default.Register<IOOMService>(() => oomService);
            SimpleIoc.Default.Register<ISmartOfflineService>(() => smartOfflineService);
            SimpleIoc.Default.Register<ISuspensionService>(() => suspensionService);
            SimpleIoc.Default.Register<IViewModelContextService>(() => viewModelContextService);

            redditService.Initialize(GetService<ISettingsService>(),  
                GetService<ISimpleHttpService>(), 
                GetService<IUserService>(), 
                GetService<INotificationService>(),
                this);
        }

        public async Task Initialize(Frame frame)
        {
            (GetService<INavigationService>() as NavigationServices).Init(frame);

            foreach (var tpl in _services)
            {
                if (tpl.Value is IBaconService)
                    await ((IBaconService)tpl.Value).Initialize(this);
            }

            //var redditService = (GetService<IRedditService>()) as OfflineDelayableRedditService;
            //await redditService.RunQueue(null);
            
        }

        private Dictionary<Type, object> _services;
        public T GetService<T>() where T : class
        {
            return _services[typeof(T)] as T;
        }

        public void AddService(Type interfaceType, object instance)
        {
            _services.Add(interfaceType, instance);
        }

        internal interface IBaconService
        {
            Task Initialize(IBaconProvider baconProvider);
        }
    }

    
}
