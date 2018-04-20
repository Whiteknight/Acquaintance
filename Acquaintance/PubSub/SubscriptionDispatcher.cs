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

        public IDisposable Subscribe<TPayload>(string[] topics, ISubscription<TPayload> subscription)
        {
            Assert.ArgumentNotNull(subscription, nameof(subscription));
            var token = _store.AddSubscription(topics, subscription);
            // TODO: Improve this logging message
            _log.Debug($"Adding subscription {subscription.Id} to type Type={typeof(TPayload).FullName}");
            return token;
        }

        public void Publish<TPayload>(string[] topics, Envelope<TPayload> envelope)
        {
            Assert.ArgumentNotNull(envelope, nameof(envelope));
            if (topics == null || topics.Length == 0)
                return;

            var subscriptions = _store.GetSubscriptions<TPayload>(topics);
            foreach (var subscription in subscriptions)
                PublishSubscription(subscription, envelope);
        }

        private void PublishSubscription<TPayload>(ISubscription<TPayload> subscription, Envelope<TPayload> topicEnvelope)
        {
            TryPublishSubscription(subscription, topicEnvelope);
            if (subscription.ShouldUnsubscribe)
                _store.Remove(subscription);
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