using Acquaintance.Threading;
using System;

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
            if (_threadPool.NumberOfRunningFreeWorkers == 0)
            {
                _act(payload);
                return;
            }

            var thread = _threadPool.GetAnyFreeWorkerThread();
            thread?.DispatchAction(new PublishEventThreadAction<TPayload>(_act, payload));
        }
    }
}