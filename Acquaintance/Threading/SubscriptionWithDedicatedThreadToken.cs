using System;

namespace Acquaintance.Threading
{
    public sealed class SubscriptionWithDedicatedThreadToken : IDisposable
    {
        private readonly IThreadPool _threadPool;
        private readonly IDisposable _token;
        private readonly int _threadId;

        public SubscriptionWithDedicatedThreadToken(IThreadPool threadPool, IDisposable token, int threadId)
        {
            _threadPool = threadPool;
            _token = token;
            _threadId = threadId;
        }

        public void Dispose()
        {
            _token.Dispose();
            _threadPool.StopDedicatedWorker(_threadId);
        }
    }
}
