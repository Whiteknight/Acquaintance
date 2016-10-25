using System;
using System.Collections.Generic;
using Acquaintance.Threading;

namespace Acquaintance.PubSub
{
    public class PubSubChannel<TPayload> : IPubSubChannel<TPayload>
    {
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly Dictionary<Guid, IPubSubSubscription<TPayload>> _subscriptions;

        public PubSubChannel(MessagingWorkerThreadPool threadPool)
        {
            _threadPool = threadPool;
            _subscriptions = new Dictionary<Guid, IPubSubSubscription<TPayload>>();
        }

        public void Publish(TPayload payload)
        {
            foreach (var subscriber in _subscriptions.Values)
                subscriber.Publish(payload);
        }

        public SubscriptionToken Subscribe(Action<TPayload> act, Func<TPayload, bool> filter, SubscribeOptions options)
        {
            var id = Guid.NewGuid();
            var subscription = CreateSubscription(act, filter, options);
            _subscriptions.Add(id, subscription);
            return new SubscriptionToken(this, id);
        }

        private IPubSubSubscription<TPayload> CreateSubscription(Action<TPayload> act, Func<TPayload, bool> filter, SubscribeOptions options)
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

        public void Unsubscribe(Guid id)
        {
            _subscriptions.Remove(id);
        }

        public void Dispose()
        {
            _subscriptions.Clear();
        }
    }
}