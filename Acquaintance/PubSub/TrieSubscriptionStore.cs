using Acquaintance.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class TrieSubscriptionStore : ISubscriptionStore, IDisposable
    {
        private readonly StringTrie<object> _channels;

        public TrieSubscriptionStore()
        {
            _channels = new StringTrie<object>();
        }

        public void Dispose()
        {
            _channels.OnEach(c => (c as IDisposable)?.Dispose());
        }

        public IDisposable AddSubscription<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            var channel = _channels.GetOrInsert(typeof(TPayload).FullName, topic.Split('.'), () => new Channel<TPayload>());
            if (channel == null)
                throw new Exception("Channel not found");
            var typedChannel = channel as Channel<TPayload>;
            if (typedChannel == null)
                throw new Exception($"Expected channel of type {typeof(TPayload).FullName} but found {channel.GetType().FullName}");
            var id = Guid.NewGuid();
            typedChannel.AddSubscription(id, subscription);

            return new SubscriptionToken<TPayload>(this, topic, id);
        }

        public IEnumerable<ISubscription<TPayload>> GetSubscriptions<TPayload>(string topic)
        {
            return _channels.Get(typeof(TPayload).FullName, topic.Split('.'))
                .OfType<Channel<TPayload>>()
                .SelectMany(c => c.GetAllSubscriptions())
                .ToArray();
        }

        private void RemoveSubscription<TPayload>(string topic, Guid id)
        {
            var root = typeof(TPayload).FullName;
            var path = topic.Split('.');
            var channel = _channels.Get(root, path);
            var typedChannel = channel as Channel<TPayload>;
            if (typedChannel == null)
                return;
            typedChannel.RemoveSubscription(id);
            if (typedChannel.IsEmpty)
                _channels.RemoveValue(root, path, v => (v as IDisposable)?.Dispose());
        }

        private class SubscriptionToken<TPayload> : IDisposable
        {
            private readonly TrieSubscriptionStore _store;
            private readonly string _topic;
            private readonly Guid _id;

            public SubscriptionToken(TrieSubscriptionStore store, string topic, Guid id)
            {
                _store = store;
                _topic = topic;
                _id = id;
            }

            public void Dispose()
            {
                _store.RemoveSubscription<TPayload>(_topic, _id);
            }
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

            public IEnumerable<ISubscription<TPayload>> GetAllSubscriptions()
            {
                return _subscriptions.Values;
            }

            public bool IsEmpty => _subscriptions.IsEmpty;
        }
    }
}