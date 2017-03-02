using System;

namespace Acquaintance.Threading
{
    public interface IThreadPool : IDisposable
    {
        int NumberOfRunningFreeWorkers { get; }
        int StartDedicatedWorker();
        void StopDedicatedWorker(int threadId);
        IActionDispatcher GetThreadDispatcher(int threadId, bool allowAutoCreate);
        IActionDispatcher GetFreeWorkerThreadDispatcher();
        IActionDispatcher GetThreadPoolActionDispatcher();
        IActionDispatcher GetAnyThreadDispatcher();
        IActionDispatcher GetCurrentThreadDispatcher();
        IMessageHandlerThreadContext GetCurrentThreadContext();

        void RegisterManagedThread(int threadId, string purpose);
        void UnregisterManagedThread(int threadId);
    }
}