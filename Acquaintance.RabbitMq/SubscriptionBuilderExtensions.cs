using System;
using Acquaintance.PubSub;

namespace Acquaintance.RabbitMq
{
    public static class SubscriptionBuilderExtensions
    {
        // TODO: Ability to specify a mapper to transform Envelope<TPayload> to some other DTO
        public static IThreadSubscriptionBuilder<TPayload> ForwardToRabbitMq<TPayload>(this IActionSubscriptionBuilder<TPayload> builder, Action<IRabbitSenderBuilder<TPayload>> setup)
        {
            var subscription = builder.MessageBus.CreateRabbitMqForwardingSubscription(setup);
            return builder.UseCustomSubscriber(subscription);
        }
    }
}