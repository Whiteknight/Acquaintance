using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class SimpleSubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<string, object> _channels;

        public SimpleSubscriptionStore()
        {
            _channels = new ConcurrentDictionary<string, object>();
        }

        public IDisposable AddSubscription<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            var key = GetKey<TPayload>(topic);
            var channelObj = _channels.GetOrAdd(key, s => new Channel<TPayload>());
            var channel = channelObj as Channel<TPayload>;
            if (channel == null)
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TPayload>)} but found {channelObj.GetType().FullName}");
            subscription.Id = Guid.NewGuid();
            channel.AddSubscription(subscription.Id, subscription);
            return new SubscriberToken<TPayload>(this, topic, subscription.Id);
        }

        public IEnumerable<ISubscription<TPayload>> GetSubscriptions<TPayload>(string topic)
        {
            var key = GetKey<TPayload>(topic);
            if (!_channels.TryGetValue(key, out object channelObj))
                return Enumerable.Empty<ISubscription<TPayload>>();
            var channel = channelObj as Channel<TPayload>;
            if (channel == null)
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TPayload>)} but found {channelObj.GetType().FullName}");
            return channel.GetSubscriptions();
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
            bool found = _channels.TryGetValue(key, out object channel);
            if (!found || channel == null)
                return;
            var typedChannel = channel as Channel<TPayload>;
            if (typedChannel == null)
                return;
            typedChannel.RemoveSubscription(id);
            if (typedChannel.IsEmpty)
                _channels.TryRemove(key, out channel);
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

            public override string ToString()
            {
                return $"Subscription Type={typeof(TPayload).Name} Topic={_topic} Id={_id}";
            }
        }
    }
}