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
            {
                for (int i = 0; i < 5; i++)
                {
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();
                }
            }

            return true;
        }

        public event Action<OutOfMemoryEventArgs> OutOfMemory;
    }
}
