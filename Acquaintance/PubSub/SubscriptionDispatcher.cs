using System;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class SubscriptionDispatcher : IDisposable
    {
        private readonly ILogger _log;
        private readonly ISubscriptionStore _store;

        public SubscriptionDispatcher(ILogger log, bool allowWildcards)
        {
            _log = log;
            _store = CreateStore(allowWildcards);
        }

        public IDisposable Subscribe<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            Assert.ArgumentNotNull(subscription, nameof(subscription));
            var token = _store.AddSubscription(topic, subscription);
            _log.Debug("Adding subscription {0} to type Type={1} Topic={2}", subscription.Id, typeof(TPayload).FullName, topic);
            return token;
        }

        public void Publish<TPayload>(string[] topics, Envelope<TPayload> envelope)
        {
            Assert.ArgumentNotNull(envelope, nameof(envelope));
            if (topics == null || topics.Length == 0)
                return;

            foreach (var topic in topics)
                PublishTopic(envelope, topic);
        }

        private void PublishTopic<TPayload>(Envelope<TPayload> envelope, string topic)
        {
            var topicEnvelope = envelope.RedirectToTopic(topic);
            var subscriptions = _store.GetSubscriptions<TPayload>(topic);
            foreach (var subscription in subscriptions)
                PublishSubscription(topic, subscription, topicEnvelope);
        }

        private void PublishSubscription<TPayload>(string topic, ISubscription<TPayload> subscription, Envelope<TPayload> topicEnvelope)
        {
            TryPublishSubscription(subscription, topicEnvelope);
            if (subscription.ShouldUnsubscribe)
                _store.Remove(topic, subscription);
        }

        private void TryPublishSubscription<TPayload>(ISubscription<TPayload> subscription, Envelope<TPayload> topicEnvelope)
        {
            if (subscription.ShouldUnsubscribe)
                return;
            try
            {
                subscription.Publish(topicEnvelope);
            }
            catch (Exception e)
            {
                _log.Error($"Error on publish Type={typeof(TPayload).FullName} Subscription Id={subscription.Id}: {e.Message}\n{e.StackTrace}");
            }
        }

        private static ISubscriptionStore CreateStore(bool allowWildcards)
        {
            if (allowWildcards)
                return new TrieSubscriptionStore();
            return new SimpleSubscriptionStore();
        }

        public void Dispose()
        {
            (_store as IDisposable)?.Dispose();
        }
    }
}