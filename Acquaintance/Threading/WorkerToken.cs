using System;

namespace Acquaintance.Threading
{
    public class WorkerToken : IDisposable
    {
        private readonly IWorkerPool _workerPool;

        public WorkerToken(IWorkerPool workerPool, int threadId)
        {
            _workerPool = workerPool;
            ThreadId = threadId;
            IsSuccess = threadId > 0;
        }

        public int ThreadId { get;  }
        public bool IsSuccess { get; }

        public void Dispose()
        {
            _workerPool.StopDedicatedWorker(ThreadId);
        }
    }
}