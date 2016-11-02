using Acquaintance.Threading;
using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public class PubSubChannel<TPayload> : IPubSubChannel<TPayload>
    {
        private readonly SubscriptionFactory _factory;
        private readonly Dictionary<Guid, ISubscription<TPayload>> _subscriptions;

        public PubSubChannel(MessagingWorkerThreadPool threadPool)
        {
            _factory = new SubscriptionFactory(threadPool);
            _subscriptions = new Dictionary<Guid, ISubscription<TPayload>>();
        }

        public void Publish(TPayload payload)
        {
            foreach (var subscriber in _subscriptions.Values)
                subscriber.Publish(payload);
        }

        public SubscriptionToken Subscribe(ISubscription<TPayload> subscription)
        {
            var id = Guid.NewGuid();
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