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
        IActionDispatcher GetCurrentThreadDispatcher();
        IMessageHandlerThreadContext GetCurrentThreadContext();
    }
}