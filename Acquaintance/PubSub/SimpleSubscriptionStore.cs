using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class SimpleSubscriptionStore : ISubscriptionStore
    {
        private readonly ConcurrentDictionary<string, object> _topicChannels;
        private readonly ConcurrentDictionary<string, object> _topiclessChannels;
        private readonly ConcurrentDictionary<Guid, string[]> _topicMap;

        public SimpleSubscriptionStore()
        {
            _topicChannels = new ConcurrentDictionary<string, object>();
            _topiclessChannels = new ConcurrentDictionary<string, object>();
            _topicMap = new ConcurrentDictionary<Guid, string[]>();
        }

        public IDisposable AddSubscription<TPayload>(string[] topics, ISubscription<TPayload> subscription)
        {
            subscription.Id = Guid.NewGuid();
            if (topics == null)
            {
                var topiclessKey = GetKey<TPayload>();
                AddSubscriptionByKey(_topiclessChannels, subscription, topiclessKey);
                return new SubscriberToken<TPayload>(this, null, subscription.Id);
            }

            topics = TopicUtility.CanonicalizeTopics(topics);

            _topicMap.TryAdd(subscription.Id, topics);
            foreach (var topic in topics)
            {
                var key = GetKey<TPayload>(topic);
                AddSubscriptionByKey(_topicChannels, subscription, key);
            }
            return new SubscriberToken<TPayload>(this, topics, subscription.Id);
        }

        public IEnumerable<ISubscription<TPayload>> GetSubscriptions<TPayload>(string[] topics)
        {
            var topiclessKey = GetKey<TPayload>();
            var topicless = GetSubscriptionsInternal<TPayload>(_topiclessChannels, topiclessKey);
            var byTopic = topics
                .Select(GetKey<TPayload>)
                .SelectMany(topicKey => GetSubscriptionsInternal<TPayload>(_topicChannels, topicKey));
            return topicless
                .Concat(byTopic)
                .Distinct();
        }

        public void Remove<TPayload>(ISubscription<TPayload> subscription)
        {
            Unsubscribe<TPayload>(subscription.Id);
        }

        private static IEnumerable<ISubscription<TPayload>> GetSubscriptionsInternal<TPayload>(ConcurrentDictionary<string, object> store, string key)
        {
            if (!store.TryGetValue(key, out object channelObj))
                return Enumerable.Empty<ISubscription<TPayload>>();
            if (!(channelObj is Channel<TPayload> channel))
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TPayload>)} but found {channelObj.GetType().FullName}");
            return channel.GetSubscriptions();
        }

        private static void AddSubscriptionByKey<TPayload>(ConcurrentDictionary<string, object> store, ISubscription<TPayload> subscription, string key)
        {
            var channelObj = store.GetOrAdd(key, s => new Channel<TPayload>());
            if (!(channelObj is Channel<TPayload> channel))
                throw new Exception($"Incorrect Channel type. Expected {typeof(Channel<TPayload>)} but found {channelObj.GetType().FullName}");
            channel.AddSubscription(subscription.Id, subscription);
        }

        private static string GetKey<TPayload>(string topic)
        {
            return $"{typeof(TPayload).FullName}:{topic}";
        }

        private static string GetKey<TPayload>()
        {
            return typeof(TPayload).FullName;
        }

        private void Unsubscribe<TPayload>(Guid id)
        {
            if (!_topicMap.TryGetValue(id, out string[] topics))
            {
                var topiclessKey = GetKey<TPayload>();
                UnsubscribeByKey<TPayload>(_topiclessChannels, id, topiclessKey);
                return;
            }

            foreach (var topic in topics)
            {
                var topicKey = GetKey<TPayload>(topic);
                UnsubscribeByKey<TPayload>(_topicChannels, id, topicKey);
            }
        }

        private static void UnsubscribeByKey<TPayload>(ConcurrentDictionary<string, object>  store, Guid id, string key)
        {
            bool found = store.TryGetValue(key, out object channel);
            if (!found || channel == null)
                return;
            if (!(channel is Channel<TPayload> typedChannel))
                return;
            typedChannel.RemoveSubscription(id);
            if (typedChannel.IsEmpty)
                store.TryRemove(key, out channel);
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
                (subscription as IDisposable)?.Dispose();
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
            private readonly string[] _topics;
            private readonly Guid _id;

            public SubscriberToken(SimpleSubscriptionStore store, string[] topics, Guid id)
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
                if (_topics != null)
                    return $"Subscription Type={typeof(TPayload).Name} Topics={string.Join(",", _topics)} Id={_id}";
                return $"Subscription Type={typeof(TPayload).Name} All Topics Id={_id}";
            }
        }
    }
}