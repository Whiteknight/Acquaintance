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
            ISubscriberReference<TPayload> actionReference = CreateActionReference(act, options);

            ISubscription<TPayload> subscription;
            switch (options.DispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    subscription = new AnyThreadPubSubSubscription<TPayload>(actionReference, _threadPool);
                    break;
                case DispatchThreadType.SpecificThread:
                    subscription = new SpecificThreadPubSubSubscription<TPayload>(actionReference, options.ThreadId, _threadPool);
                    break;
                default:
                    subscription = new ImmediatePubSubSubscription<TPayload>(actionReference);
                    break;
            }

            if (filter != null)
                subscription = new FilteredSubscription<TPayload>(subscription, filter);
            if (options.MaxEvents > 0)
                subscription = new MaxEventsSubscription<TPayload>(subscription, options.MaxEvents);
            return subscription;
        }

        private static ISubscriberReference<TPayload> CreateActionReference<TPayload>(Action<TPayload> act, SubscribeOptions options)
        {
            if (options.KeepAlive)
                return new StrongSubscriberReference<TPayload>(act);
            return new WeakSubscriberReference<TPayload>(act);
        }
    }
}