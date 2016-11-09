using System;

namespace Acquaintance
{
    public static class EavesdropMessageBusExtensions
    {
        public static IDisposable Eavesdrop<TRequest, TResponse>(this IListenable messageBus, string name, Action<Conversation<TRequest, TResponse>> subscriber, Func<Conversation<TRequest, TResponse>, bool> filter = null, SubscribeOptions options = null)
        {
            var subscription = messageBus.SubscriptionFactory.CreateSubscription(subscriber, filter, options);
            return messageBus.Eavesdrop(name, subscription);
        }

        public static IDisposable Eavesdrop<TRequest, TResponse>(this IListenable messageBus, Action<Conversation<TRequest, TResponse>> subscriber, Func<Conversation<TRequest, TResponse>, bool> filter = null, SubscribeOptions options = null)
        {
            return messageBus.Eavesdrop(string.Empty, subscriber, filter, options);
        }
    }
}