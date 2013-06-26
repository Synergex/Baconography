using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface ISuspensionService
    {
        event Action Suspending;
        event Action Resuming;

        void FireSuspending();
        void FireResuming();
    }
}
