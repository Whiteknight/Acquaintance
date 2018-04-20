using Acquaintance.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class TrieSubscriptionStore : ISubscriptionStore, IDisposable
    {
        private readonly StringTrie<object> _topicChannels;
        private readonly ConcurrentDictionary<string, object> _topiclessChannels;
        private readonly ConcurrentDictionary<Guid, string[]> _topicMap;

        public TrieSubscriptionStore()
        {
            _topicChannels = new StringTrie<object>();
            _topiclessChannels = new ConcurrentDictionary<string, object>();
            _topicMap = new ConcurrentDictionary<Guid, string[]>();
        }

        public IDisposable AddSubscription<TPayload>(string[] topics, ISubscription<TPayload> subscription)
        {
            subscription.Id = Guid.NewGuid();
            if (topics == null)
            {
                AddAllTopicsSubscription(subscription);
                return new SubscriptionToken<TPayload>(this, null, subscription.Id);
            }

            if (topics.Length == 0)
                topics = new[] { string.Empty };

            _topicMap.TryAdd(subscription.Id, topics);
            foreach (var topic in topics)
                AddTopicSubscription(subscription, topic);

            return new SubscriptionToken<TPayload>(this, topics, subscription.Id);
        }

        public IEnumerable<ISubscription<TPayload>> GetSubscriptions<TPayload>(string[] topics)
        {
            var topicless = GetTopiclessSubscriptions<TPayload>();

            var byTopic = topics.SelectMany(GetTopicSubscriptions<TPayload>);
            
            return topicless
                .Concat(byTopic)
                .Distinct();
        }

        public void Remove<TPayload>(ISubscription<TPayload> subscription)
        {
            Unsubscribe<TPayload>(subscription.Id);
        }

        public void Dispose()
        {
            _topicChannels.OnEach(c => (c as IDisposable)?.Dispose());
        }

        private void AddTopicSubscription<TPayload>(ISubscription<TPayload> subscription, string topic)
        {
            var channel = _topicChannels.GetOrInsert(typeof(TPayload).FullName, topic.Split('.'), () => new Channel<TPayload>());
            if (channel == null)
                throw new Exception("Channel not found");
            if (!(channel is Channel<TPayload> typedChannel))
                throw new Exception($"Expected channel of type {typeof(TPayload).FullName} but found {channel.GetType().FullName}");
            typedChannel.AddSubscription(subscription.Id, subscription);
        }

        private void AddAllTopicsSubscription<TPayload>(ISubscription<TPayload> subscription)
        {
            var key = typeof(TPayload).FullName;
            var channelObj = _topiclessChannels.GetOrAdd(key, s => new Channel<TPayload>());
            if (!(channelObj is Channel<TPayload> channel))
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TPayload>)} but found {channelObj.GetType().FullName}");
            channel.AddSubscription(subscription.Id, subscription);
        }

        private IEnumerable<ISubscription<TPayload>> GetTopicSubscriptions<TPayload>(string topic)
        {
            return _topicChannels.Get(typeof(TPayload).FullName, topic.Split('.'))
                .OfType<Channel<TPayload>>()
                .SelectMany(c => c.GetAllSubscriptions())
                .ToArray();
        }

        private IEnumerable<ISubscription<TPayload>> GetTopiclessSubscriptions<TPayload>()
        {
            var key = typeof(TPayload).FullName;
            if (!_topiclessChannels.TryGetValue(key, out object channelObj))
                return Enumerable.Empty<ISubscription<TPayload>>();
            if (!(channelObj is Channel<TPayload> channel))
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TPayload>)} but found {channelObj.GetType().FullName}");
            return channel.GetAllSubscriptions();
        }

        private void Unsubscribe<TPayload>(Guid id)
        {
            if (!_topicMap.TryGetValue(id, out string[] topics))
            {
                UnsubscribeTopicless<TPayload>(id);
                return;
            }

            foreach (var topic in topics)
                UnsubscribeByTopic<TPayload>(topic, id);
        }

        private void UnsubscribeTopicless<TPayload>(Guid id)
        {
            var key = typeof(TPayload).FullName;
            bool found = _topiclessChannels.TryGetValue(key, out object channel);
            if (!found || channel == null)
                return;
            if (!(channel is Channel<TPayload> typedChannel))
                return;
            typedChannel.RemoveSubscription(id);
            if (typedChannel.IsEmpty)
                _topiclessChannels.TryRemove(key, out channel);
        }

        private void UnsubscribeByTopic<TPayload>(string topic, Guid id)
        {
            var root = typeof(TPayload).FullName;
            var path = topic.Split('.');
            var channel = _topicChannels.Get(root, path).FirstOrDefault();
            if (!(channel is Channel<TPayload> typedChannel))
                return;
            typedChannel.RemoveSubscription(id);
            if (typedChannel.IsEmpty)
                _topicChannels.RemoveValue(root, path, v => (v as IDisposable)?.Dispose());
        }

        private class SubscriptionToken<TPayload> : IDisposable
        {
            private readonly TrieSubscriptionStore _store;
            private readonly string[] _topics;
            private readonly Guid _id;

            public SubscriptionToken(TrieSubscriptionStore store, string[] topics, Guid id)
            {
                _store = store;
                _topics = topics;
                _id = id;
            }

            public void Dispose()
            {
                _store.Unsubscribe<TPayload>(_id);
            }

            public override string ToString()
            {
                if (_topics == null)
                    return $"Subscription Type={typeof(TPayload).Name} All Topics Id={_id}";
                return $"Subscription Type={typeof(TPayload).Name} Topics={string.Join(",", _topics)} Id={_id}";
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