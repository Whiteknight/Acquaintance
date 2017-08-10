using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class PubSubChannel<TPayload> : IPubSubChannel<TPayload>
    {
        private readonly ILogger _log;
        private readonly ConcurrentDictionary<Guid, ISubscription<TPayload>> _subscriptions;

        public PubSubChannel(ILogger log)
        {
            _log = log ?? new SilentLogger();
            _subscriptions = new ConcurrentDictionary<Guid, ISubscription<TPayload>>();
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public void Publish(Envelope<TPayload> message)
        {
            var toUnsubscribe = new List<Guid>();
            foreach (var kvp in _subscriptions)
            {
                try
                {
                    var subscriber = kvp.Value;
                    subscriber.Publish(message);
                    if (subscriber.ShouldUnsubscribe)
                        toUnsubscribe.Add(kvp.Key);
                }
                catch (Exception e)
                {
                    _log.Error("Error on publish to subscription {0}: {1}\n{2}", kvp.Key, e.Message, e.StackTrace);
                }
            }
            foreach (var id in toUnsubscribe)
                Unsubscribe(id);
        }

        public SubscriptionToken Subscribe(ISubscription<TPayload> subscription)
        {
            Assert.ArgumentNotNull(subscription, nameof(subscription));

            var id = Guid.NewGuid();
            _subscriptions.TryAdd(id, subscription);
            return new SubscriptionToken(this, id);
        }

        public void Unsubscribe(Guid id)
        {
            _subscriptions.TryRemove(id, out ISubscription<TPayload> subscription);
        }

        public void Dispose()
        {
            _subscriptions.Clear();
        }
    }
}