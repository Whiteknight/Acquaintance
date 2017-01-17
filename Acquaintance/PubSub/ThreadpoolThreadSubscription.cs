using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class ThreadPoolThreadSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly IThreadPool _threadPool;
        private readonly ISubscriberReference<TPayload> _action;

        public ThreadPoolThreadSubscription(IThreadPool threadPool, ISubscriberReference<TPayload> action)
        {
            if (threadPool == null)
                throw new System.ArgumentNullException(nameof(threadPool));

            if (action == null)
                throw new System.ArgumentNullException(nameof(action));

            _threadPool = threadPool;
            _action = action;
        }

        public Guid Id { get; set; }
        public bool ShouldUnsubscribe => false;

        public void Publish(TPayload payload)
        {
            var action = new PublishEventThreadAction<TPayload>(_action, payload);
            var context = _threadPool.GetThreadPoolActionDispatcher();
            context.DispatchAction(action);
        }
    }
}