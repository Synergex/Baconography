using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaconographyPortable.Services.Impl
{
    public class SuspensionService : ISuspensionService
    {
        public event Action Suspending;
        public event Action Resuming;
        private CancellationTokenSource _suspensionCancelToken = new CancellationTokenSource();

        public void FireSuspending()
        {
            try
            {
                Suspending();
                var suspendableWorkQueue = ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>();
                suspendableWorkQueue.RaiseImpendingSuspension();
            }
            catch { }
        }

        public void FireResuming()
        {
            try
            {
                var suspendableWorkQueue = ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>();
                suspendableWorkQueue.CancelSuspension();
                if (Resuming != null)
                    Resuming();
            }
            catch { }
        }
    }
}
