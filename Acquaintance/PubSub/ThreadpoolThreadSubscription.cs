using Acquaintance.Threading;
using System;
using System.Threading.Tasks;

namespace Acquaintance.PubSub
{
    public class ThreadpoolThreadSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly MessagingWorkerThreadPool _threadPool;

        public ThreadpoolThreadSubscription(ISubscriberReference<TPayload> action)
        {
            _action = action;
        }

        public bool ShouldUnsubscribe => false;

        public void Publish(TPayload payload)
        {
            var action = new PublishEventThreadAction<TPayload>(_action, payload);
            Task.Factory.StartNew(() =>
            {
                try
                {
                    action.Execute(null);
                }
                catch (Exception e)
                {
                    // TODO: Log it or inform the user somehow?
                }
            });
        }
    }
}