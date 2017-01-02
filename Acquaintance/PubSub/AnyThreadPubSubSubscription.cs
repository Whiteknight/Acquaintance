using Acquaintance.Threading;

namespace Acquaintance.PubSub
{
    public class AnyThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly IThreadPool _threadPool;

        public AnyThreadPubSubSubscription(ISubscriberReference<TPayload> action, IThreadPool threadPool)
        {
            _action = action;
            _threadPool = threadPool;
        }

        public void Publish(TPayload payload)
        {
            var thread = _threadPool.GetFreeWorkerThreadDispatcher();
            if (thread == null)
            {
                _action.Invoke(payload);
                return;
            }
            thread.DispatchAction(new PublishEventThreadAction<TPayload>(_action, payload));
        }

        public bool ShouldUnsubscribe => false;
    }
}