using Acquaintance.Threading;
using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class AnyThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly IThreadPool _threadPool;

        public AnyThreadPubSubSubscription(ISubscriberReference<TPayload> action, IThreadPool threadPool)
        {
            Assert.ArgumentNotNull(action, nameof(action));
            Assert.ArgumentNotNull(threadPool, nameof(threadPool));
            
            _action = action;
            _threadPool = threadPool;
        }

        public Guid Id { get; set; }

        public void Publish(Envelope<TPayload> message)
        {
            var thread = _threadPool.GetAnyThreadDispatcher();
            thread.DispatchAction(new PublishEventThreadAction<TPayload>(_action, message));
        }

        public bool ShouldUnsubscribe => false;
    }
}