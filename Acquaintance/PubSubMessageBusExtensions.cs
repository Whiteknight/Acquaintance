using Acquaintance.PubSub;
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

        public static IDisposable Subscribe<TPayload>(this IPubSubBus messageBus, Action<IChannelSubscriptionBuilder<TPayload>> build)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            var builder = new SubscriptionBuilder<TPayload>(messageBus, messageBus.ThreadPool);
            build(builder);
            var subscription = builder.BuildSubscription();

            var token = messageBus.Subscribe<TPayload>(builder.ChannelName, subscription);
            return builder.WrapToken(token);
        }
    }
}