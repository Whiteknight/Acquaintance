using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class SpecificThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly Action<TPayload> _act;
        private readonly Func<TPayload, bool> _filter;
        private readonly int _threadId;
        private readonly MessagingWorkerThreadPool _threadPool;

        public SpecificThreadPubSubSubscription(Action<TPayload> act, Func<TPayload, bool> filter, int threadId, MessagingWorkerThreadPool threadPool)
        {
            _act = act;
            _filter = filter;
            _threadId = threadId;
            _threadPool = threadPool;
        }

        public void Publish(TPayload payload)
        {
            if (_filter != null && !_filter(payload))
                return;
            var thread = _threadPool.GetThread(_threadId, true);
            thread?.DispatchAction(new PublishEventThreadAction<TPayload>(_act, payload));
        }
    }
}