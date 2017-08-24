using System;

namespace Acquaintance.Threading
{
    public interface IWorkerPool
    {
        int NumberOfRunningFreeWorkers { get; }
        ThreadReport GetThreadReport();
        WorkerToken StartDedicatedWorker();
        void StopDedicatedWorker(int threadId);
        IActionDispatcher GetDispatcher(int threadId, bool allowAutoCreate);
        IActionDispatcher GetFreeWorkerDispatcher();
        IActionDispatcher GetThreadPoolDispatcher();
        IActionDispatcher GetAnyWorkerDispatcher();
        IActionDispatcher GetCurrentThreadDispatcher();
        IWorkerContext GetCurrentThreadContext();

        IDisposable RegisterManagedThread(IThreadManager manager, int threadId, string purpose);
        void UnregisterManagedThread(int threadId);
    }
}