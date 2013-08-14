using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaconographyPortable.Services.Impl
{
    public class SuspendableWorkQueueImpl : ISuspendableWorkQueue
    {
        ISystemServices _systemServices;
        public SuspendableWorkQueueImpl(ISystemServices systemServices)
        {
            _systemServices = systemServices;
        }

        Queue<Tuple<TaskCompletionSource<bool>, Func<CancellationToken, Task>>> _lowImportanceRestartableWork = new Queue<Tuple<TaskCompletionSource<bool>,Func<CancellationToken,Task>>>();
        
        public IHVOToken HighValueOperationToken
        {
            get { return new HVOTokenImpl(this); }
        }

        internal int HVOCount { get; set; }

        private class HVOTokenImpl : IHVOToken
        {
            SuspendableWorkQueueImpl _workQueue;
            public HVOTokenImpl(SuspendableWorkQueueImpl workQueue) 
            {
                _workQueue = workQueue;
                _workQueue.HVOCount++;
            }
            ~HVOTokenImpl()
            {
                if (_workQueue != null)
                {
                    _workQueue.HVOCount--;
                    _workQueue = null;
                }
            }
            public void Dispose()
            {
                _workQueue.HVOCount--;
                _workQueue = null;
            }
        }

        bool _taskQueueRunning = false;

        public Task QueueLowImportanceRestartableWork(Func<System.Threading.CancellationToken, Task> work)
        {
            bool needsToStartTaskQueue = false;
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
            lock (_lowImportanceRestartableWork)
            {
                _lowImportanceRestartableWork.Enqueue(Tuple.Create(completionSource, work));

                if (_lowImportanceRestartableWork.Count == 1 || !_taskQueueRunning)
                {
                    needsToStartTaskQueue = true;
                    _taskQueueRunning = true;
                }
            }

            if (needsToStartTaskQueue)
            {
                Task.Factory.StartNew(RunWorkQueue, _cancelationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            return completionSource.Task;
        }
        CancellationTokenSource _cancelationTokenSource = new CancellationTokenSource();

        private async void RunWorkQueue()
        {
            try
            {
                Tuple<TaskCompletionSource<bool>, Func<CancellationToken, Task>> workUnit = null;
                do
                {
                    lock (_lowImportanceRestartableWork)
                    {
                        if (_lowImportanceRestartableWork.Count > 0)
                        {
                            workUnit = _lowImportanceRestartableWork.Dequeue();
                        }
                        else
                            workUnit = null;
                    }

                    if (workUnit != null)
                    {
                        try
                        {
                            while (HVOCount > 0)
                            {
                                await Task.Delay(100);
                            }

                            await workUnit.Item2(_cancelationTokenSource.Token);
                            if (_cancelationTokenSource.IsCancellationRequested)
                                workUnit.Item1.SetCanceled();
                            else
                                workUnit.Item1.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            workUnit.Item1.SetException(ex);
                        }

                    }
                } while (workUnit != null && !_cancelationTokenSource.IsCancellationRequested);
            }
            finally
            {
                _taskQueueRunning = false;
            }
        }

        object _cancelationTimer = null;
        public void RaiseImpendingSuspension()
        {
            //start timer, give things 6 seconds of nothing new being created then set the cancelation token
            lock (this)
            {
                if (_cancelationTimer == null)
                {
                    _cancelationTimer = _systemServices.StartTimer((obj, obj2) =>
                        {
                            lock (this)
                            {
                                _systemServices.StopTimer(_cancelationTimer);
                                _cancelationTimer = null;
                                _cancelationTokenSource.Cancel();
                                _uiCancelationTokenSource.Cancel();
                            }

                        }, TimeSpan.FromSeconds(6), false);
                }
            }
        }

        public void CancelSuspension()
        {
            lock (this)
            {
                //if the cancelation token hasnt been set yet, just kill the timer
                //otherwise we need to revive things
                if (_cancelationTimer != null)
                    _systemServices.StopTimer(_cancelationTimer);

                _cancelationTimer = null;
            }
        }

        CancellationTokenSource _uiCancelationTokenSource = new CancellationTokenSource();
        Queue<Func<CancellationToken, Task>> _uiInteruptableQueue = new Queue<Func<CancellationToken, Task>>();
        bool _uiTaskQueueRunning = false;
        public void QueueInteruptableUI(Func<CancellationToken, Task> work)
        {
            bool needsToStartTaskQueue = false;
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
            lock (_uiInteruptableQueue)
            {
                _uiInteruptableQueue.Enqueue(work);

                if (_uiInteruptableQueue.Count == 1 || !_uiTaskQueueRunning)
                {
                    needsToStartTaskQueue = true;
                    _uiTaskQueueRunning = true;
                }
            }

            if (needsToStartTaskQueue)
            {
                _systemServices.StartTimer((timer, arg) =>
                    {
                        _systemServices.StopTimer(timer);
                        RunUIWorkQueue();
                    }, TimeSpan.FromMilliseconds(0), true);
            }
        }

        private async void RunUIWorkQueue()
        {
            try
            {
                Func<CancellationToken, Task> workUnit = null;
                do
                {
                    lock (_uiInteruptableQueue)
                    {
                        if (_uiInteruptableQueue.Count > 0)
                        {
                            workUnit = _uiInteruptableQueue.Dequeue();
                        }
                        else
                            workUnit = null;
                    }

                    if (workUnit != null)
                    {
                        try
                        {
                            while (HVOCount > 0)
                            {
                                await Task.Delay(100);
                            }

                            await workUnit(_cancelationTokenSource.Token);
                        }
                        catch { }

                    }
                } while (workUnit != null && !_cancelationTokenSource.IsCancellationRequested);
            }
            finally
            {
                _uiTaskQueueRunning = false;
            }
        }

        public void RaiseUIInterupt()
        {
            _uiCancelationTokenSource.Cancel();
            _uiCancelationTokenSource = new CancellationTokenSource();
        }
    }
}
