using Acquaintance.PubSub;
using System;
using Acquaintance.Utility;

namespace Acquaintance
{
    public static class EavesdropMessageBusExtensions
    {
        /// <summary>
        /// Build an eavesdrop subscription
        /// </summary>
        /// <typeparam name="TRequest">The type of request object</typeparam>
        /// <typeparam name="TResponse">The type of response object</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="build">Lambda function to build up the subscription with common options.</param>
        /// <returns>A token which represents the subscription. When disposed, the subscription is cancelled.</returns>
        public static IDisposable Eavesdrop<TRequest, TResponse>(this IMessageBus messageBus, Action<SubscriptionBuilder<Conversation<TRequest, TResponse>>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(build, nameof(build));

            var builder = new SubscriptionBuilder<Conversation<TRequest, TResponse>>(messageBus, messageBus.ThreadPool);
            build(builder);
            var subscription = builder.BuildSubscription();

            var token = messageBus.Eavesdrop(builder.Topic, subscription);
            return builder.WrapToken(token);
        }
    }
}