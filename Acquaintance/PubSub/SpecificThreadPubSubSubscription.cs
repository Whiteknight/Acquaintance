using Acquaintance.Threading;
using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class SpecificThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly int _threadId;
        private readonly IWorkerPool _workerPool;

        public SpecificThreadPubSubSubscription(ISubscriberReference<TPayload> action, int threadId, IWorkerPool workerPool)
        {
            Assert.ArgumentNotNull(action, nameof(action));
            Assert.ArgumentNotNull(workerPool, nameof(workerPool));

            _action = action;
            _threadId = threadId;
            _workerPool = workerPool;
        }

        public Guid Id { get; set; }

        public void Publish(Envelope<TPayload> message)
        {
            var thread = _workerPool.GetDispatcher(_threadId, true);
            thread?.DispatchAction(new PublishEventThreadAction<TPayload>(_action, message));
        }

        public bool ShouldUnsubscribe => false;
    }
}
