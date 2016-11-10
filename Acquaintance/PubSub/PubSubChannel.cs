using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
            List<Guid> toUnsubscribe = new List<Guid>();
            foreach (var kvp in _subscriptions)
            {
                try
                {
                    var subscriber = kvp.Value;
                    subscriber.Publish(payload);
                    if (subscriber.ShouldUnsubscribe)
                        toUnsubscribe.Add(kvp.Key);
                }
                catch { }
            }
            foreach (var id in toUnsubscribe)
                Unsubscribe(id);
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