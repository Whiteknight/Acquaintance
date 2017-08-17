using System;

namespace Acquaintance.Threading
{
    public interface IThreadPool
    {
        int NumberOfRunningFreeWorkers { get; }
        ThreadReport GetThreadReport();
        ThreadToken StartDedicatedWorker();
        void StopDedicatedWorker(int threadId);
        IActionDispatcher GetThreadDispatcher(int threadId, bool allowAutoCreate);
        IActionDispatcher GetFreeWorkerThreadDispatcher();
        IActionDispatcher GetThreadPoolActionDispatcher();
        IActionDispatcher GetAnyThreadDispatcher();
        IActionDispatcher GetCurrentThreadDispatcher();
        IMessageHandlerThreadContext GetCurrentThreadContext();

        IDisposable RegisterManagedThread(IThreadManager manager, int threadId, string purpose);
        void UnregisterManagedThread(int threadId);
    }
}