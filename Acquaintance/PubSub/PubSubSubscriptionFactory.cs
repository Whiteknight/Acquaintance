using Acquaintance.Threading;
using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public class PubSubSubscriptionFactory
    {
        private readonly MessagingWorkerThreadPool _threadPool;

        public PubSubSubscriptionFactory(MessagingWorkerThreadPool threadPool)
        {
            _threadPool = threadPool;
        }

        public IPubSubSubscription<TPayload> CreateSubscription<TPayload>(Action<TPayload> act, Func<TPayload, bool> filter, SubscribeOptions options)
        {
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