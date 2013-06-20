using BaconographyPortable.Model.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface ISmartOfflineService
    {
        void MaybeSuspend();
        void MaybeWakeup();

        void NavigatedToOfflinableThing(Thing targetThing);

        //need to know what the current network status is
        //need to know when links/comments/images are clicked
        //needs to manage suspending itself and wakeing up with slightly different behavior if it wakes back up quickly
    }
}
