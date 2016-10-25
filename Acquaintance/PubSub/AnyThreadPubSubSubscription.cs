using System;
using Acquaintance.Threading;

namespace Acquaintance.PubSub
{
    public class AnyThreadPubSubSubscription<TPayload> : IPubSubSubscription<TPayload>
    {
        private readonly Action<TPayload> _act;
        private readonly Func<TPayload, bool> _filter;
        private readonly MessagingWorkerThreadPool _threadPool;

        public AnyThreadPubSubSubscription(Action<TPayload> act, Func<TPayload, bool> filter, MessagingWorkerThreadPool threadPool)
        {
            _act = act;
            _filter = filter;
            _threadPool = threadPool;
        }

        public void Publish(TPayload payload)
        {
            if (_filter != null && !_filter(payload))
                return;
            var thread = _threadPool.GetAnyThread();
            if (thread == null)
                return;
            thread.DispatchAction(new PublishEventThreadAction<TPayload>(_act, payload));
        }
    }
}