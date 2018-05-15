using Acquaintance.Threading;
using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class ThreadPoolThreadSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly IWorkerPool _workerPool;
        private readonly ISubscriberReference<TPayload> _action;

        public ThreadPoolThreadSubscription(IWorkerPool workerPool, ISubscriberReference<TPayload> action)
        {
            Assert.ArgumentNotNull(workerPool, nameof(workerPool));
            Assert.ArgumentNotNull(action, nameof(action));

            _workerPool = workerPool;
            _action = action;
        }

        public Guid Id { get; set; }
        public bool ShouldUnsubscribe => false;

        public void Publish(Envelope<TPayload> message)
        {
            var action = new PublishEventThreadAction<TPayload>(_action, message);
            var context = _workerPool.GetThreadPoolDispatcher();
            context.DispatchAction(action);
        }

        public void Dispose()
        {
        }
    }
}