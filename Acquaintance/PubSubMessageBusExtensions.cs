using Acquaintance.PubSub;
using System;

namespace Acquaintance
{
    public static class PubSubMessageBusExtensions
    {
        public static void Publish<TPayload>(this IPublishable messageBus, TPayload payload)
        {
            messageBus.Publish(string.Empty, payload);
        }

        public static void Publish(this IPublishable messageBus, string name, Type payloadType, object payload)
        {
            var method = messageBus.GetType().GetMethod("Publish").MakeGenericMethod(payloadType);
            method.Invoke(messageBus, new[] { name, payload });
        }

        public static IDisposable Subscribe<TPayload>(this ISubscribable messageBus, string name, Action<TPayload> subscriber, SubscribeOptions options = null)
        {
            return messageBus.Subscribe(name, subscriber, null, options);
        }

        public static IDisposable Subscribe<TPayload>(this ISubscribable messageBus, Action<TPayload> subscriber, SubscribeOptions options = null)
        {
            return messageBus.Subscribe(string.Empty, subscriber, null, options);
        }

        public static IDisposable Subscribe<TPayload>(this ISubscribable messageBus, Action<TPayload> subscriber, Func<TPayload, bool> filter, SubscribeOptions options = null)
        {
            return messageBus.Subscribe(string.Empty, subscriber, filter, options);
        }

        public static SubscriptionBuilder<TPayload> CreateSubscription<TPayload>(this ISubscribable messageBus, Action<TPayload> subscriber)
        {
            return new SubscriptionBuilder<TPayload>(messageBus, subscriber);
        }

        public static IDisposable Transform<TInput, TOutput>(this IPubSubBus messageBus, string inName, Func<TInput, TOutput> transform, Func<TInput, bool> filter, string outName, SubscribeOptions options = null)
        {
            return messageBus.Subscribe(inName, input =>
            {
                TOutput output = transform(input);
                messageBus.Publish(outName, output);
            }, filter, options);
        }
    }
}