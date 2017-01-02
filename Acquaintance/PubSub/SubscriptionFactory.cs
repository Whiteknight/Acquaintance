using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class SubscriptionFactory
    {
        private readonly IThreadPool _threadPool;

        public SubscriptionFactory(IThreadPool threadPool)
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
                case DispatchThreadType.ThreadpoolThread:
                    subscription = new ThreadpoolThreadSubscription<TPayload>(actionReference);
                    break;
                case DispatchThreadType.Immediate:
                    subscription = new ImmediatePubSubSubscription<TPayload>(actionReference);
                    break;
                default:
                    subscription = CreateDefaultSubscription<TPayload>(actionReference, _threadPool);
                    break;
            }

            if (filter != null)
                subscription = new FilteredSubscription<TPayload>(subscription, filter);
            if (options.MaxEvents > 0)
                subscription = new MaxEventsSubscription<TPayload>(subscription, options.MaxEvents);
            return subscription;
        }

        private static ISubscription<TPayload> CreateDefaultSubscription<TPayload>(ISubscriberReference<TPayload> actionReference, IThreadPool threadPool)
        {
            if (threadPool != null && threadPool.NumberOfRunningFreeWorkers > 0)
                return new AnyThreadPubSubSubscription<TPayload>(actionReference, threadPool);
            return new ThreadpoolThreadSubscription<TPayload>(actionReference);
        }

        private static ISubscriberReference<TPayload> CreateActionReference<TPayload>(Action<TPayload> act, SubscribeOptions options)
        {
            if (options.KeepAlive)
                return new StrongSubscriberReference<TPayload>(act);
            return new WeakSubscriberReference<TPayload>(act);
        }
    }
}