using Acquaintance.PubSub;
using System;

namespace Acquaintance
{
    public static class PubSubMessageBusExtensions
    {
        /// <summary>
        /// Publish the given payload as an event on the default channel
        /// </summary>
        /// <typeparam name="TPayload">The type of payload object</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="payload">The payload object to publish as an event. This object should not
        /// be modified after publishing, to prevent concurrency conflicts.</param>
        public static void Publish<TPayload>(this IPublishable messageBus, TPayload payload)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            messageBus.Publish(string.Empty, payload);
        }

        /// <summary>
        /// Publish the given payload as an event on the channel. 
        /// This overload is used when the object type is not known at compile time but is only
        /// available at runtime.
        /// </summary>
        /// <param name="messageBus">The message bus</param>
        /// <param name="channelName">The name of the channel</param>
        /// <param name="payloadType">The runtime-known type of the payload object</param>
        /// <param name="payload">The payload object itself.</param>
        public static void Publish(this IPublishable messageBus, string channelName, Type payloadType, object payload)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            var method = messageBus.GetType().GetMethod("Publish").MakeGenericMethod(payloadType);
            method.Invoke(messageBus, new[] { channelName, payload });
        }

        /// <summary>
        /// Build a subscription using common options
        /// </summary>
        /// <typeparam name="TPayload">The type of object to listen for</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="build">Lambda function to setup the subscription builder.</param>
        /// <returns>The subscription token which, when disposed, cancels the subscription.</returns>
        public static IDisposable Subscribe<TPayload>(this IPubSubBus messageBus, Action<IChannelSubscriptionBuilder<TPayload>> build)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (build == null)
                throw new ArgumentNullException(nameof(build));

            var builder = new SubscriptionBuilder<TPayload>(messageBus, messageBus.ThreadPool);
            build(builder);
            var subscription = builder.BuildSubscription();

            var token = messageBus.Subscribe(builder.ChannelName, subscription);
            return builder.WrapToken(token);
        }
    }
}