using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services.Impl
{
    public class SuspensionService : ISuspensionService
    {
        public event Action Suspending;

        public event Action Resuming;

        public void FireSuspending()
        {
            if (Suspending != null)
                Suspending();
        }

        public void FireResuming()
        {
            if (Resuming != null)
                Resuming();
        }
    }
}
