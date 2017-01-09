using Acquaintance.PubSub;
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
            var subscription = builder.BuildSubscription();

            return messageBus.Eavesdrop<TRequest, TResponse>(builder.ChannelName, subscription);
        }
    }
}