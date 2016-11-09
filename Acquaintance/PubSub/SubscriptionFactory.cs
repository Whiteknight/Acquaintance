using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class SubscriptionFactory
    {
        private readonly MessagingWorkerThreadPool _threadPool;

        public SubscriptionFactory(MessagingWorkerThreadPool threadPool)
        {
            _threadPool = threadPool;
        }

        public ISubscription<TPayload> CreateSubscription<TPayload>(Action<TPayload> act, Func<TPayload, bool> filter, SubscribeOptions options)
        {
            options = options ?? SubscribeOptions.Default;
            ISubscription<TPayload> subscription;
            switch (options.DispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    subscription = new AnyThreadPubSubSubscription<TPayload>(act, _threadPool);
                    break;
                case DispatchThreadType.SpecificThread:
                    subscription = new SpecificThreadPubSubSubscription<TPayload>(act, options.ThreadId, _threadPool);
                    break;
                default:
                    subscription = new ImmediatePubSubSubscription<TPayload>(act);
                    break;
            }

            if (filter != null)
                subscription = new FilteredSubscription<TPayload>(subscription, filter);
            if (options.MaxEvents > 0)
                subscription = new MaxEventsSubscription<TPayload>(subscription, options.MaxEvents);
            return subscription;
        }
    }
}