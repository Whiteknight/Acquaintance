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

        public static IDisposable Subscribe<TPayload>(this ISubscribable messageBus, string name, Action<TPayload> subscriber, Func<TPayload, bool> filter, SubscribeOptions options = null)
        {
            var subscription = messageBus.SubscriptionFactory.CreateSubscription(subscriber, filter, options);
            return messageBus.Subscribe<TPayload>(name, subscription);
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

        public static IDisposable SubscribeTransform<TInput, TOutput>(this IPubSubBus messageBus, string inName, Func<TInput, TOutput> transform, Func<TInput, bool> filter, string outName = null, SubscribeOptions options = null)
        {
            return messageBus.Subscribe(inName, input =>
            {
                TOutput output = transform(input);
                messageBus.Publish(outName, output);
            }, filter, options);
        }

        public static EventRouter<TPayload> SubscriptionRouter<TPayload>(this IPubSubBus messageBus, string channelName)
        {
            var router = new EventRouter<TPayload>(messageBus, channelName);
            var token = messageBus.Subscribe(channelName, router);
            router.SetToken(token);
            return router;
        }
    }
}