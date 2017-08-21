using System;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class SubscriptionDispatcher 
    {
        private readonly ILogger _log;
        private readonly ISubscriptionStore _store;

        public SubscriptionDispatcher(ILogger log, bool allowWildcards)
        {
            _log = log;
            _store = allowWildcards ? (ISubscriptionStore)new TrieSubscriptionStore() : new SimpleSubscriptionStore();
        }

        public IDisposable Subscribe<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            Assert.ArgumentNotNull(subscription, nameof(subscription));
            return _store.AddSubscription(topic, subscription);
        }

        public void Publish<TPayload>(string[] topics, Envelope<TPayload> envelope)
        {
            Assert.ArgumentNotNull(envelope, nameof(envelope));
            if (topics == null || topics.Length == 0)
                return;
            foreach (var topic in topics)
            {
                var topicEnvelope = topic == envelope.Topic ? envelope : envelope.RedirectToTopic(topic);
                var subscribers = _store.GetSubscriptions<TPayload>(topic);
                foreach (var subscriber in subscribers)
                {
                    try
                    {
                        subscriber.Publish(topicEnvelope);
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Error on publish Type={typeof(TPayload).FullName} Subscription Id={subscriber.Id}: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }

        public void Dispose()
        {
            (_store as IDisposable)?.Dispose();
        }
    }
}