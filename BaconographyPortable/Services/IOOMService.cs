using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public class OutOfMemoryEventArgs
    {
        public bool HasCleanedUp { get; set; }
        public bool IsEmergency { get; internal set; }
    }
    public interface IOOMService
    {
        //returns true if we were able to clean anything up
        bool TryToCleanup(bool emergency, bool forceGC);
        //called when we need to clean up, handlers can set HasCleanedUp if they were able to free anything
        //GC.Collect will be called when these all finish
        event Action<OutOfMemoryEventArgs> OutOfMemory;
    }
}
