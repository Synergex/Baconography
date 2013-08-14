using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IHVOToken : IDisposable { }
    public interface ISuspendableWorkQueue
    {
        Task QueueLowImportanceRestartableWork(Func<CancellationToken, Task> work);
        void QueueInteruptableUI(Func<CancellationToken, Task> work);
        void RaiseImpendingSuspension();
        void RaiseUIInterupt();
        void CancelSuspension();
        IHVOToken HighValueOperationToken { get; }
    }
}
