using System;

namespace Acquaintance.Threading
{
    public sealed class SubscriptionWithDedicatedWorkerToken : IDisposable
    {
        private readonly IWorkerPool _workerPool;
        private readonly IDisposable _subscriptionToken;
        private readonly WorkerToken _workerToken;

        public SubscriptionWithDedicatedWorkerToken(IWorkerPool workerPool, IDisposable subscriptionToken, int threadId)
        {
            _workerToken = new WorkerToken(workerPool, threadId);
            _subscriptionToken = subscriptionToken;
        }

        public void Dispose()
        {
            _subscriptionToken.Dispose();
            _workerToken.Dispose();
        }

        public override string ToString()
        {
            return _subscriptionToken.ToString() + "\n" + _workerToken.ToString();
        }
    }
}
