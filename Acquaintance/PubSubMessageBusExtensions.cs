using Acquaintance.PubSub;
using Acquaintance.Utility;
using System;

namespace Acquaintance
{
    public static class PubSubMessageBusExtensions
    {
        public static void Publish<TPayload>(this IPublishable messageBus, TPayload payload)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            messageBus.Publish(string.Empty, payload);
        }

        public static void Publish(this IPublishable messageBus, string name, Type payloadType, object payload)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            var method = messageBus.GetType().GetMethod("Publish").MakeGenericMethod(payloadType);
            method.Invoke(messageBus, new[] { name, payload });
        }

        public static IDisposable Subscribe<TPayload>(this IPubSubBus messageBus, Action<SubscriptionBuilder<TPayload>> build)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            var builder = new SubscriptionBuilder<TPayload>(messageBus, messageBus.ThreadPool);
            build(builder);
            var subscriptions = builder.BuildSubscriptions();

            if (subscriptions.Count == 1)
                return messageBus.Subscribe<TPayload>(builder.ChannelName, subscriptions[0]);

            var disposables = new DisposableCollection();
            foreach (var subscription in subscriptions)
            {
                var token = messageBus.Subscribe<TPayload>(builder.ChannelName, subscription);
                disposables.Add(token);
            }
            return disposables;
        }
    }
}