using Acquaintance.Threading;
using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public sealed class AnyThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly IWorkerPool _workerPool;

        public AnyThreadPubSubSubscription(ISubscriberReference<TPayload> action, IWorkerPool workerPool)
        {
            Assert.ArgumentNotNull(action, nameof(action));
            Assert.ArgumentNotNull(workerPool, nameof(workerPool));

            _action = action;
            _workerPool = workerPool;
        }

        public Guid Id { get; set; }

        public void Publish(Envelope<TPayload> message)
        {
            var thread = _workerPool.GetAnyWorkerDispatcher();
            thread.DispatchAction(new PublishEventThreadAction<TPayload>(_action, message));
        }

        public bool ShouldUnsubscribe => false;

        public void Dispose()
        {
        }
    }
}