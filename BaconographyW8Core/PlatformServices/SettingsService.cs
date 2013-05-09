using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyW8.PlatformServices
{
    class SettingsService : ISettingsService, BaconProvider.IBaconService
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

        public void ShowSettings()
        {
            
        }

        public async Task Initialize(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            try
            {
                Messenger.Default.Register<ConnectionStatusMessage>(this, OnConnectionStatusChanged);

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
            }
            catch
            {
                //not interested in failure here
            }
        }

        private void OnConnectionStatusChanged(ConnectionStatusMessage obj)
        {
            if (obj.IsOnline)
                SetOnline(obj.UserInitiated);
            else
                SetOffline(obj.UserInitiated);
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
        }


        public async Task ClearHistory()
        {
            var offlineService = _baconProvider.GetService<IOfflineService>();
            await offlineService.ClearHistory();
        }



		public bool LeftHandedMode
		{
			get;
			set;
		}
	}
}
