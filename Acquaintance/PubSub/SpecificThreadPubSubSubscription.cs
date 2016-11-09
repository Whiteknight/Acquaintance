using Acquaintance.Threading;

namespace Acquaintance.PubSub
{
    public class SpecificThreadPubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly int _threadId;
        private readonly MessagingWorkerThreadPool _threadPool;

        public SpecificThreadPubSubSubscription(ISubscriberReference<TPayload> action, int threadId, MessagingWorkerThreadPool threadPool)
        {
            _action = action;
            _threadId = threadId;
            _threadPool = threadPool;
        }

        public void Publish(TPayload payload)
        {
            var thread = _threadPool.GetThreadDispatcher(_threadId, true);
            thread?.DispatchAction(new PublishEventThreadAction<TPayload>(_action, payload));
        }

        public bool ShouldUnsubscribe => false;
    }
}