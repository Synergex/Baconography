using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services.Impl
{
    public class OOMService : IOOMService
    {
        //need to keep this around since we wont be able to allocate it when its needed
        OutOfMemoryEventArgs args = new OutOfMemoryEventArgs();
        public bool TryToCleanup(bool emergency, bool forceGC)
        {
            args.HasCleanedUp = false;
            args.IsEmergency = emergency;
            if (OutOfMemory != null)
                OutOfMemory(args);
            if (forceGC)
                GC.Collect(2, GCCollectionMode.Forced, true);

            return args.HasCleanedUp;
        }

        public event Action<OutOfMemoryEventArgs> OutOfMemory;
    }
}
