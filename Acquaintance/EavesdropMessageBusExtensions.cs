using Acquaintance.PubSub;
using Acquaintance.Utility;
using System;

namespace Acquaintance
{
    public static class EavesdropMessageBusExtensions
    {
        public static IDisposable Eavesdrop<TRequest, TResponse>(this IMessageBus messageBus, Action<SubscriptionBuilder<Conversation<TRequest, TResponse>>> build)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            var builder = new SubscriptionBuilder<Conversation<TRequest, TResponse>>(messageBus, messageBus.ThreadPool);
            build(builder);
            var subscriptions = builder.BuildSubscriptions();
            if (subscriptions.Count == 1)
                return messageBus.Eavesdrop<TRequest, TResponse>(builder.ChannelName, subscriptions[0]);

            var disposables = new DisposableCollection();
            foreach (var subscription in subscriptions)
            {
                var token = messageBus.Eavesdrop<TRequest, TResponse>(builder.ChannelName, subscription);
                disposables.Add(token);
            }
            return disposables;
        }
    }
}