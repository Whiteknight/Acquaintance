using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class SpecificThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly Action<TPayload> _act;
        private readonly int _threadId;
        private readonly MessagingWorkerThreadPool _threadPool;

        public SpecificThreadPubSubSubscription(Action<TPayload> act, int threadId, MessagingWorkerThreadPool threadPool)
        {
            _act = act;
            _threadId = threadId;
            _threadPool = threadPool;
        }

        public void Publish(TPayload payload)
        {
            var thread = _threadPool.GetThread(_threadId, true);
            thread?.DispatchAction(new PublishEventThreadAction<TPayload>(_act, payload));
        }
    }
}