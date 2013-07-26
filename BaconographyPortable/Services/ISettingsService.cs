using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface ISettingsService
    {
        bool IsOnline();
        void SetOffline(bool fromUser);
        void SetOnline(bool fromUser);

        bool AllowOver18 {get; set;}
        bool AllowOver18Items { get; set; }
        int MaxTopLevelOfflineComments { get; set; }
        bool OfflineOnlyGetsFirstSet { get; set; }
        bool OpenLinksInBrowser { get; set; }
        bool HighlightAlreadyClickedLinks { get; set; }
        bool ApplyReadabliltyToLinks { get; set; }
        bool PreferImageLinksForTiles { get; set; }
		bool LeftHandedMode { get; set; }
		bool OrientationLock { get; set; }
		string Orientation { get; set; }
        bool AllowPredictiveOffliningOnMeteredConnection { get; set; }
        bool AllowPredictiveOfflining { get; set; }
        bool PromptForCaptcha { get; set; }
        bool HighresLockScreenOnly { get; set; }
        bool EnableUpdates { get; set; }
        bool EnableOvernightUpdates { get; set; }
        bool UpdateOverlayOnlyOnWifi { get; set; }
        bool UpdateImagesOnlyOnWifi { get; set; }
        bool MessagesInLockScreenOverlay { get; set; }
        bool PostsInLockScreenOverlay { get; set; }
        string ImagesSubreddit { get; set; }
        int OverlayOpacity { get; set; }
        int OverlayItemCount { get; set; }
        string LockScreenReddit { get; set; }
        string LiveTileReddit { get; set; }
        int OfflineCacheDays { get; set; }
        bool TapForComments { get; set; }

        bool AllowAdvertising { get; set; }

        int ScreenWidth { get; set; }
        int ScreenHeight { get; set; }

        void ShowSettings();
        Task Persist();
        Task ClearHistory();
        int DefaultOfflineLinkCount { get; set; }
    }
}
