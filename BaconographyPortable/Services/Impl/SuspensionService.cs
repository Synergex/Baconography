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
            var cancelToken = _suspensionCancelToken.Token;
            var systemServices = ServiceLocator.Current.GetInstance<ISystemServices>();
            systemServices.StartTimer((obj, obj2) =>
                {
                    try
                    {
                        systemServices.StopTimer(obj);
                        if (!cancelToken.IsCancellationRequested && Suspending != null)
                            Suspending();

                        var suspendableWorkQueue = ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>();
                        suspendableWorkQueue.RaiseImpendingSuspension();
                        
                    }
                    catch { }
                }, TimeSpan.FromSeconds(6), false);
            
        }

        public void FireResuming()
        {
            try
            {
                var suspendableWorkQueue = ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>();
                suspendableWorkQueue.CancelSuspension();
                _suspensionCancelToken.Cancel();
                _suspensionCancelToken = new CancellationTokenSource();
                if (Resuming != null)
                    Resuming();
            }
            catch { }
        }
    }
}
