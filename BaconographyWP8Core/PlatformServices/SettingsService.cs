using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.PlatformServices
{
    public class SettingsService : ISettingsService, BaconProvider.IBaconService
    {
        IBaconProvider _baconProvider;
        bool _isOnline = true;
        public bool IsOnline()
        {
            return _isOnline;
        }

        public void SetOffline(bool fromUser)
        {
            _isOnline = false;
        }

        public void SetOnline(bool fromUser)
        {
            _isOnline = true;
        }

        public bool AllowOver18 { get; set; }
        public int MaxTopLevelOfflineComments { get; set; }
        public bool OfflineOnlyGetsFirstSet { get; set; }
        public bool OpenLinksInBrowser { get; set; }
        public bool HighlightAlreadyClickedLinks { get; set; }
        public bool ApplyReadabliltyToLinks {get; set;}
        public bool PreferImageLinksForTiles { get; set; }
        public int DefaultOfflineLinkCount { get; set; }
		public bool LeftHandedMode{ get; set; }
		public bool OrientationLock { get; set; }
		public string Orientation { get; set; }
        public bool AllowPredictiveOfflining { get; set; }
        public bool PromptForCaptcha { get; set; }
        public bool AllowOver18Items { get; set; }
        public bool AllowPredictiveOffliningOnMeteredConnection { get; set; }
        public bool HighresLockScreenOnly { get; set; }
        public bool EnableUpdates { get; set; }
        public bool MessagesInLockScreenOverlay { get; set; }
        public bool PostsInLockScreenOverlay { get; set; }
        public int OverlayOpacity { get; set; }
        public string ImagesSubreddit { get; set; }
        public bool EnableLockScreenImages { get; set; }
        public string LockScreenReddit { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }

        public bool PredictiveOffline { get; set; }
        public bool IntensiveOffline { get; set; }
        public int OverlayItemCount { get; set; }
        public int OfflineCacheDays { get; set; }

        public void ShowSettings()
        {
            
        }

        public async Task Initialize(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            try
            {
                var offlineService = _baconProvider.GetService<IOfflineService>();

                var allowOver18String = await offlineService.GetSetting("AllowOver18");
                if (!string.IsNullOrWhiteSpace(allowOver18String))
                    AllowOver18 = bool.Parse(allowOver18String);
                else
                    AllowOver18 = false;

                var maxTopLevelOfflineCommentsString = await offlineService.GetSetting("MaxTopLevelOfflineComments");
                if (!string.IsNullOrWhiteSpace(maxTopLevelOfflineCommentsString))
                    MaxTopLevelOfflineComments = int.Parse(maxTopLevelOfflineCommentsString);
                else
                    MaxTopLevelOfflineComments = 50;

                var offlineOnlyGetsFirstSetString = await offlineService.GetSetting("OfflineOnlyGetsFirstSet");
                if (!string.IsNullOrWhiteSpace(offlineOnlyGetsFirstSetString))
                    OfflineOnlyGetsFirstSet = bool.Parse(offlineOnlyGetsFirstSetString);
                else
                    OfflineOnlyGetsFirstSet = true;

                var openLinksInBrowserString = await offlineService.GetSetting("OpenLinksInBrowser");
                if (!string.IsNullOrWhiteSpace(openLinksInBrowserString))
                    OpenLinksInBrowser = bool.Parse(openLinksInBrowserString);
                else
                    OpenLinksInBrowser = false;

                var highlightAlreadyClickedLinksString = await offlineService.GetSetting("HighlightAlreadyClickedLinks");
                if (!string.IsNullOrWhiteSpace(highlightAlreadyClickedLinksString))
                    HighlightAlreadyClickedLinks = bool.Parse(highlightAlreadyClickedLinksString);
                else
                    HighlightAlreadyClickedLinks = true;

                var applyReadabliltyToLinksString = await offlineService.GetSetting("ApplyReadabliltyToLinks");
                if (!string.IsNullOrWhiteSpace(allowOver18String))
                    ApplyReadabliltyToLinks = bool.Parse(applyReadabliltyToLinksString);
                else
                    ApplyReadabliltyToLinks = false;

                var preferImageLinksForTiles = await offlineService.GetSetting("PreferImageLinksForTiles");
                if (!string.IsNullOrWhiteSpace(preferImageLinksForTiles))
                    PreferImageLinksForTiles = bool.Parse(preferImageLinksForTiles);
                else
                    PreferImageLinksForTiles = true;

                var defaultOfflineLinkCount = await offlineService.GetSetting("DefaultOfflineLinkCount");
                if (!string.IsNullOrWhiteSpace(defaultOfflineLinkCount))
                    DefaultOfflineLinkCount = int.Parse(defaultOfflineLinkCount);
                else
                    DefaultOfflineLinkCount = 25;

				var leftHandedMode = await offlineService.GetSetting("LeftHandedMode");
				if (!string.IsNullOrWhiteSpace(leftHandedMode))
					LeftHandedMode = bool.Parse(leftHandedMode);
				else
					LeftHandedMode = false;

				var orientationLock = await offlineService.GetSetting("OrientationLock");
				if (!string.IsNullOrWhiteSpace(orientationLock))
					OrientationLock = bool.Parse(orientationLock);
				else
					OrientationLock = false;

				var orientation = await offlineService.GetSetting("Orientation");
				if (!string.IsNullOrWhiteSpace(orientation))
					Orientation = orientation;
				else
					Orientation = "";

                var predicitveOfflining = await offlineService.GetSetting("AllowPredictiveOfflining");
                if (!string.IsNullOrWhiteSpace(predicitveOfflining))
                    AllowPredictiveOfflining = bool.Parse(predicitveOfflining);
                else
                    AllowPredictiveOfflining = false;

                PromptForCaptcha = true;
                var over18Items = await offlineService.GetSetting("AllowOver18Items");
                if (!string.IsNullOrWhiteSpace(over18Items))
                    AllowOver18Items = bool.Parse(over18Items);
                else
                    AllowOver18Items = false;

                var predictiveOffliningOnMeteredConnection = await offlineService.GetSetting("AllowPredictiveOffliningOnMeteredConnection");
                if (!string.IsNullOrWhiteSpace(predictiveOffliningOnMeteredConnection))
                    AllowPredictiveOffliningOnMeteredConnection = bool.Parse(predictiveOffliningOnMeteredConnection);
                else
                    AllowPredictiveOffliningOnMeteredConnection = false;

                var overlayOpacity = await offlineService.GetSetting("OverlayOpacity");
                if (!string.IsNullOrWhiteSpace(overlayOpacity))
                    OverlayOpacity = int.Parse(overlayOpacity);
                else
                    OverlayOpacity = 40;

                var highresLockScreenOnly = await offlineService.GetSetting("HighresLockScreenOnly");
                if (!string.IsNullOrWhiteSpace(highresLockScreenOnly))
                    HighresLockScreenOnly = bool.Parse(highresLockScreenOnly);
                else
                    HighresLockScreenOnly = false;

                var messagesInLockScreenOverlay = await offlineService.GetSetting("MessagesInLockScreenOverlay");
                if (!string.IsNullOrWhiteSpace(messagesInLockScreenOverlay))
                    MessagesInLockScreenOverlay = bool.Parse(messagesInLockScreenOverlay);
                else
                    MessagesInLockScreenOverlay = true;

                var enableUpdates = await offlineService.GetSetting("EnableUpdates");
                if (!string.IsNullOrWhiteSpace(enableUpdates))
                    EnableUpdates = bool.Parse(enableUpdates);
                else
                    EnableUpdates = false;

                var postsInLockScreenOverlay = await offlineService.GetSetting("PostsInLockScreenOverlay");
                if (!string.IsNullOrWhiteSpace(postsInLockScreenOverlay))
                    PostsInLockScreenOverlay = bool.Parse(postsInLockScreenOverlay);
                else
                    PostsInLockScreenOverlay = true;

                var imagesSubreddit = await offlineService.GetSetting("ImagesSubreddit");
				if (!string.IsNullOrWhiteSpace(imagesSubreddit))
					ImagesSubreddit = imagesSubreddit;
				else
                    ImagesSubreddit = "/r/earthporn";

                var lockScreenReddit = await offlineService.GetSetting("LockScreenReddit");
				if (!string.IsNullOrWhiteSpace(lockScreenReddit))
					LockScreenReddit = lockScreenReddit;
				else
					LockScreenReddit = "/";

                var overlayItemCount = await offlineService.GetSetting("OverlayItemCount");
                if (!string.IsNullOrWhiteSpace(overlayItemCount))
                    OverlayItemCount = int.Parse(overlayItemCount);
                else
                    OverlayItemCount = 5;

                var offlineCacheDays = await offlineService.GetSetting("OfflineCacheDays");
                if (!string.IsNullOrWhiteSpace(offlineCacheDays))
                    OfflineCacheDays = int.Parse(offlineCacheDays);
                else
                    OfflineCacheDays = 2;

                var predictiveOffline = await offlineService.GetSetting("PredictiveOffline");
                if (!string.IsNullOrWhiteSpace(predictiveOffline))
                    PredictiveOffline = bool.Parse(predictiveOffline);
                else
                    PredictiveOffline = false;

                var intensiveOffline = await offlineService.GetSetting("IntensiveOffline");
                if (!string.IsNullOrWhiteSpace(intensiveOffline))
                    IntensiveOffline = bool.Parse(intensiveOffline);
                else
                    IntensiveOffline = false;



				Messenger.Default.Send<SettingsChangedMessage>(new SettingsChangedMessage { InitialLoad = true });
            }
            catch
            {
                //not interested in failure here
            }
        }

        public async Task Persist()
        {
            var offlineService = _baconProvider.GetService<IOfflineService>();

            await offlineService.StoreSetting("AllowOver18", AllowOver18.ToString());
            await offlineService.StoreSetting("MaxTopLevelOfflineComments", MaxTopLevelOfflineComments.ToString());
            await offlineService.StoreSetting("OfflineOnlyGetsFirstSet", OfflineOnlyGetsFirstSet.ToString());
            await offlineService.StoreSetting("OpenLinksInBrowser", OpenLinksInBrowser.ToString());
            await offlineService.StoreSetting("HighlightAlreadyClickedLinks", HighlightAlreadyClickedLinks.ToString());
            await offlineService.StoreSetting("ApplyReadabliltyToLinks", ApplyReadabliltyToLinks.ToString());
            await offlineService.StoreSetting("PreferImageLinksForTiles", PreferImageLinksForTiles.ToString());
            await offlineService.StoreSetting("DefaultOfflineLinkCount", DefaultOfflineLinkCount.ToString());
			await offlineService.StoreSetting("LeftHandedMode", LeftHandedMode.ToString());
			await offlineService.StoreSetting("OrientationLock", OrientationLock.ToString());
			await offlineService.StoreSetting("Orientation", Orientation.ToString());
            await offlineService.StoreSetting("AllowPredictiveOfflining", AllowPredictiveOfflining.ToString());
            await offlineService.StoreSetting("AllowPredictiveOffliningOnMeteredConnection", AllowPredictiveOffliningOnMeteredConnection.ToString());
            await offlineService.StoreSetting("AllowOver18Items", AllowOver18Items.ToString());
            await offlineService.StoreSetting("OverlayOpacity", OverlayOpacity.ToString());
            await offlineService.StoreSetting("HighresLockScreenOnly", HighresLockScreenOnly.ToString());
            await offlineService.StoreSetting("PredictiveOffline", PredictiveOffline.ToString());
            await offlineService.StoreSetting("IntensiveOffline", IntensiveOffline.ToString());
            await offlineService.StoreSetting("OverlayItemCount", OverlayItemCount.ToString());
            await offlineService.StoreSetting("OfflineCacheDays", OfflineCacheDays.ToString());
        }


        public async Task ClearHistory()
        {
            var offlineService = _baconProvider.GetService<IOfflineService>();
            await offlineService.ClearHistory();
        }
    }
}
