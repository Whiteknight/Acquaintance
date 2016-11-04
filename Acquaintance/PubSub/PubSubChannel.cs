using System;
using System.Collections.Concurrent;

namespace Acquaintance.PubSub
{
    public class PubSubChannel<TPayload> : IPubSubChannel<TPayload>
    {
        private readonly ConcurrentDictionary<Guid, ISubscription<TPayload>> _subscriptions;

        public PubSubChannel()
        {
            _subscriptions = new ConcurrentDictionary<Guid, ISubscription<TPayload>>();
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public void Publish(TPayload payload)
        {
            foreach (var subscriber in _subscriptions.Values)
                subscriber.Publish(payload);
        }

        public SubscriptionToken Subscribe(ISubscription<TPayload> subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            var id = Guid.NewGuid();
            _subscriptions.TryAdd(id, subscription);
            return new SubscriptionToken(this, id);
        }

        public void Unsubscribe(Guid id)
        {
            ISubscription<TPayload> subscription;
            _subscriptions.TryRemove(id, out subscription);
        }

        public void Dispose()
        {
            _subscriptions.Clear();
        }
    }
}