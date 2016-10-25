using System;

namespace Acquaintance
{
    public static class PubSubMessageBusExtensions
    {
        public static void Publish<TPayload>(this IMessageBus messageBus, TPayload payload)
        {
            messageBus.Publish(string.Empty, payload);
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

        public static IDisposable Transform<TInput, TOutput>(this IMessageBus messageBus, string inName, Func<TInput, TOutput> transform, Func<TInput, bool> filter, string outName, SubscribeOptions options = null)
        {
            return messageBus.Subscribe(inName, input => {
                TOutput output = transform(input);
                messageBus.Publish(outName, output);
            }, filter, options);
        }
    }
}