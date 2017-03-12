﻿using Acquaintance.PubSub;
using System;

namespace Acquaintance
{
    public static class PubSubMessageBusExtensions
    {
        /// <summary>
        /// Publish an event of the given type on the given channel
        /// </summary>
        /// <typeparam name="TPayload">The type of event payload to publish</typeparam>
        /// <param name="channelName">The name of the channel</param>
        /// <param name="payload">The event payload object to send to subscribers. This object should not be modified after publishing to avoid concurrency conflicts.</param>
        public static void Publish<TPayload>(this IPubSubBus messageBus, string channelName, TPayload payload)
        {
            var envelope = messageBus.EnvelopeFactory.Create<TPayload>(channelName, payload);
            messageBus.PublishEnvelope<TPayload>(envelope);
        }

        /// <summary>
        /// Publish the given payload as an event on the default channel
        /// </summary>
        /// <typeparam name="TPayload">The type of payload object</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="payload">The payload object to publish as an event. This object should not
        /// be modified after publishing, to prevent concurrency conflicts.</param>
        public static void Publish<TPayload>(this IPubSubBus messageBus, TPayload payload)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            Publish(messageBus, string.Empty, payload);
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
        public static void Publish(this IPubSubBus messageBus, string channelName, Type payloadType, object payload)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            var factoryMethod = messageBus.EnvelopeFactory.GetType()
                .GetMethod(nameof(messageBus.EnvelopeFactory.Create))
                .MakeGenericMethod(payloadType);
            var envelope = factoryMethod.Invoke(messageBus.EnvelopeFactory, new[] { channelName, payload, null });

            var method = messageBus.GetType().GetMethod(nameof(messageBus.PublishEnvelope)).MakeGenericMethod(payloadType);
            method.Invoke(messageBus, new[] { envelope });
        }

        /// <summary>
        /// Publish an encapsulated message to the bus
        /// </summary>
        /// <param name="messageBus">The message bus</param>
        /// <param name="message">The encapsulated message with all necessary publish details</param>
        public static void PublishMessage(this IPubSubBus messageBus, IPublishableMessage message)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            message.PublishTo(messageBus);
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