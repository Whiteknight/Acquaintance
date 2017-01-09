using Acquaintance.Threading;

namespace Acquaintance.PubSub
{
    public class ThreadPoolThreadSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly IThreadPool _threadPool;
        private readonly ISubscriberReference<TPayload> _action;

        public ThreadPoolThreadSubscription(IThreadPool threadPool, ISubscriberReference<TPayload> action)
        {
            _threadPool = threadPool;
            _action = action;
        }

        public bool ShouldUnsubscribe => false;

        public void Publish(TPayload payload)
        {
            var action = new PublishEventThreadAction<TPayload>(_action, payload);
            var context = _threadPool.GetThreadPoolActionDispatcher();
            context.DispatchAction(action);
        }
    }
}