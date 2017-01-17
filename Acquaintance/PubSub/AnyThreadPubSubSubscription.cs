using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class AnyThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly IThreadPool _threadPool;

        public AnyThreadPubSubSubscription(ISubscriberReference<TPayload> action, IThreadPool threadPool)
        {
            if (action == null)
                throw new System.ArgumentNullException(nameof(action));

            if (threadPool == null)
                throw new System.ArgumentNullException(nameof(threadPool));

            _action = action;
            _threadPool = threadPool;
        }

        public Guid Id { get; set; }

        public void Publish(TPayload payload)
        {
            var thread = _threadPool.GetAnyThreadDispatcher();
            thread.DispatchAction(new PublishEventThreadAction<TPayload>(_action, payload));
        }

        public bool ShouldUnsubscribe => false;
    }
}