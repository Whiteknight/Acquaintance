using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class SpecificThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly int _threadId;
        private readonly IThreadPool _threadPool;

        public SpecificThreadPubSubSubscription(ISubscriberReference<TPayload> action, int threadId, IThreadPool threadPool)
        {
            if (action == null)
                throw new System.ArgumentNullException(nameof(action));

            if (threadPool == null)
                throw new System.ArgumentNullException(nameof(threadPool));

            _action = action;
            _threadId = threadId;
            _threadPool = threadPool;
        }

        public Guid Id { get; set; }

        public void Publish(TPayload payload)
        {
            var thread = _threadPool.GetThreadDispatcher(_threadId, true);
            thread?.DispatchAction(new PublishEventThreadAction<TPayload>(_action, payload));
        }

        public bool ShouldUnsubscribe => false;
    }
}