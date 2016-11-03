using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class AnyThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly Action<TPayload> _act;
        private readonly MessagingWorkerThreadPool _threadPool;

        public AnyThreadPubSubSubscription(Action<TPayload> act, MessagingWorkerThreadPool threadPool)
        {
            _act = act;
            _threadPool = threadPool;
        }

        public void Publish(TPayload payload)
        {
            if (_threadPool.NumberOfRunningFreeWorkers == 0)
            {
                _act(payload);
                return;
            }

            var thread = _threadPool.GetAnyFreeWorkerThread();
            if (thread == null)
            {
                _act(payload);
                return;
            }
            thread.DispatchAction(new PublishEventThreadAction<TPayload>(_act, payload));
        }
    }
}