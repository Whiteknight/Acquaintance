using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public class SimpleSubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<string, object> _pubSubChannels;

        public SimpleSubscriptionStore()
        {
            _pubSubChannels = new ConcurrentDictionary<string, object>();
        }

        public IDisposable AddSubscription<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            var key = GetKey<TPayload>(topic);
            var channel = _pubSubChannels.GetOrAdd(key, s => new Channel<TPayload>());
            var typedChannel = channel as Channel<TPayload>;
            if (typedChannel == null)
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TPayload>)} but found {channel.GetType().FullName}");
            var id = Guid.NewGuid();
            typedChannel.AddSubscription(id, subscription);
            return new SubscriberToken<TPayload>(this, topic, id);
        }

        public IEnumerable<ISubscription<TPayload>> GetSubscriptions<TPayload>(string topic)
        {
            var key = GetKey<TPayload>(topic);
            var channel = _pubSubChannels.GetOrAdd(key, s => new Channel<TPayload>());
            var typedChannel = channel as Channel<TPayload>;
            if (typedChannel == null)
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TPayload>)} but found {channel.GetType().FullName}");
            return typedChannel.GetSubscriptions();
        }

        public void Remove<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            Unsubscribe<TPayload>(topic, subscription.Id);
        }

        private static string GetKey<TPayload>(string topic)
        {
            return $"{typeof(TPayload).FullName}:{topic}";
        }

        private void Unsubscribe<TPayload>(string topic, Guid id)
        {
            var key = GetKey<TPayload>(topic);
            bool found = _pubSubChannels.TryGetValue(key, out object channel);
            if (!found || channel == null)
                return;
            var typedChannel = channel as Channel<TPayload>;
            if (typedChannel == null)
                return;
            typedChannel.RemoveSubscription(id);
            if (typedChannel.IsEmpty)
                _pubSubChannels.TryRemove(key, out channel);
        }

        private class Channel<TPayload> 
        {
            private readonly ConcurrentDictionary<Guid, ISubscription<TPayload>> _subscriptions;

            public Channel()
            {
                _subscriptions = new ConcurrentDictionary<Guid, ISubscription<TPayload>>();
            }

            public void AddSubscription(Guid id, ISubscription<TPayload> subscription)
            {
                _subscriptions.TryAdd(id, subscription);
            }

            public void RemoveSubscription(Guid id)
            {
                _subscriptions.TryRemove(id, out ISubscription<TPayload> subscription);
            }

            public IEnumerable<ISubscription<TPayload>> GetSubscriptions()
            {
                return _subscriptions.Values;
            }

            public bool IsEmpty => _subscriptions.IsEmpty;
        }

        private class SubscriberToken<TPayload> : IDisposable
        {
            private readonly SimpleSubscriptionStore _store;
            private readonly string _topic;
            private readonly Guid _id;

            public SubscriberToken(SimpleSubscriptionStore store, string topic, Guid id)
            {
                _store = store;
                _topic = topic;
                _id = id;
            }

            public void Dispose()
            {
                _store.Unsubscribe<TPayload>(_topic, _id);
            }
        }
    }
}