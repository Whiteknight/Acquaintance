using Acquaintance.Threading;
using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public class PubSubChannel<TPayload> : IPubSubChannel<TPayload>
    {
        private readonly PubSubSubscriptionFactory _factory;
        private readonly Dictionary<Guid, IPubSubSubscription<TPayload>> _subscriptions;

        public PubSubChannel(MessagingWorkerThreadPool threadPool)
        {
            _factory = new PubSubSubscriptionFactory(threadPool);
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
            var subscription = _factory.CreateSubscription(act, filter, options);
            _subscriptions.Add(id, subscription);
            return new SubscriptionToken(this, id);
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