using System;

namespace Acquaintance.Threading
{
    public sealed class SubscriptionWithDedicatedWorkerToken : IDisposable
    {
        private readonly IWorkerPool _workerPool;
        private readonly IDisposable _token;
        private readonly int _threadId;

        public SubscriptionWithDedicatedWorkerToken(IWorkerPool workerPool, IDisposable token, int threadId)
        {
            _workerPool = workerPool;
            _token = token;
            _threadId = threadId;
        }

        public void Dispose()
        {
            _token.Dispose();
            _workerPool.StopDedicatedWorker(_threadId);
        }
    }
}
