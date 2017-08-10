using Acquaintance.Threading;
using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class ThreadPoolThreadSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly IThreadPool _threadPool;
        private readonly ISubscriberReference<TPayload> _action;

        public ThreadPoolThreadSubscription(IThreadPool threadPool, ISubscriberReference<TPayload> action)
        {
            Assert.ArgumentNotNull(threadPool, nameof(threadPool));
            Assert.ArgumentNotNull(action, nameof(action));

            _threadPool = threadPool;
            _action = action;
        }

        public Guid Id { get; set; }
        public bool ShouldUnsubscribe => false;

        public void Publish(Envelope<TPayload> message)
        {
            var action = new PublishEventThreadAction<TPayload>(_action, message);
            var context = _threadPool.GetThreadPoolActionDispatcher();
            context.DispatchAction(action);
        }
    }
}