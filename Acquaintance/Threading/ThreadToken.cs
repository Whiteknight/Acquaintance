using System;

namespace Acquaintance.Threading
{
    public class ThreadToken : IDisposable
    {
        private readonly IThreadPool _threadPool;

        public ThreadToken(IThreadPool threadPool, int threadId)
        {
            _threadPool = threadPool;
            ThreadId = threadId;
            IsSuccess = threadId > 0;
        }

        public int ThreadId { get;  }
        public bool IsSuccess { get; }

        public void Dispose()
        {
            _threadPool.StopDedicatedWorker(ThreadId);
        }
    }
}