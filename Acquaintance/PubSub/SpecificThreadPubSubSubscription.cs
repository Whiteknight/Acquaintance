using Acquaintance.Threading;
using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class SpecificThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly int _threadId;
        private readonly IThreadPool _threadPool;

        public SpecificThreadPubSubSubscription(ISubscriberReference<TPayload> action, int threadId, IThreadPool threadPool)
        {
            Assert.ArgumentNotNull(action, nameof(action));
            Assert.ArgumentNotNull(threadPool, nameof(threadPool));

            _action = action;
            _threadId = threadId;
            _threadPool = threadPool;
        }

        public Guid Id { get; set; }

        public void Publish(Envelope<TPayload> message)
        {
            var thread = _threadPool.GetThreadDispatcher(_threadId, true);
            thread?.DispatchAction(new PublishEventThreadAction<TPayload>(_action, message));
        }

        public bool ShouldUnsubscribe => false;
    }
}
