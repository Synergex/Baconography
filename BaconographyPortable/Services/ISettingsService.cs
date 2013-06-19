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
        int MaxTopLevelOfflineComments { get; set; }
        bool OfflineOnlyGetsFirstSet { get; set; }
        bool OpenLinksInBrowser { get; set; }
        bool HighlightAlreadyClickedLinks { get; set; }
        bool ApplyReadabliltyToLinks { get; set; }
        bool PreferImageLinksForTiles { get; set; }
		bool LeftHandedMode { get; set; }
		bool OrientationLock { get; set; }
		string Orientation { get; set; }
        bool AllowPredictiveOfflining { get; set; }

        void ShowSettings();
        Task Persist();
        Task ClearHistory();
        int DefaultOfflineLinkCount { get; set; }
    }
}
