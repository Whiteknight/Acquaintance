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
            switch (options.DispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    return new AnyThreadPubSubSubscription<TPayload>(act, filter, _threadPool);
                case DispatchThreadType.SpecificThread:
                    return new SpecificThreadPubSubSubscription<TPayload>(act, filter, options.ThreadId, _threadPool);
                default:
                    return new ImmediatePubSubSubscription<TPayload>(act, filter);
            }
        }
    }
}