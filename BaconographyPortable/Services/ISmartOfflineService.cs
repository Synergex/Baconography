using BaconographyPortable.Model.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public enum OffliningOpportunityPriority
    {
        Image,
        Thumbnail,
        Links,
        Comments,
        ImageAPI,
        None
    }

    public enum NetworkConnectivityStatus
    {
        Unmetered,
        Metered,
        Wifi,
        Unknown
    }

    public interface ISmartOfflineService
    {
        void NavigatedToOfflineableThing(Thing targetThing, bool link);
        void NavigatedToView(Type viewType, bool forward);
        void ClearOfflineData();

        IEnumerable<string> OfflineableImagesFromContext { get; }
        IEnumerable<string> OfflineableImageAPIsFromContext { get; }
        IEnumerable<string> OfflineableLinksFromContext { get; }
        IEnumerable<TypedThing<Link>> OfflineableLinkThingsFromContext { get; }
        bool IsActivityIdle { get; }

        event Action<OffliningOpportunityPriority, NetworkConnectivityStatus, CancellationToken> OffliningOpportunity;
    }
}
