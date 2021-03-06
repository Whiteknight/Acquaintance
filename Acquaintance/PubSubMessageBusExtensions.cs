﻿using Acquaintance.PubSub;
using System;
using System.Reflection;
using Acquaintance.Scanning;
using Acquaintance.Utility;

namespace Acquaintance
{
    public static class PubSubMessageBusExtensions
    {
        /// <summary>
        /// Publish an event of the given type on the given topic
        /// </summary>
        /// <typeparam name="TPayload">The type of event payload to publish</typeparam>
        /// <param name="messageBus"></param>
        /// <param name="topic">The name of the channel</param>
        /// <param name="payload">The event payload object to send to subscribers. This object should not be modified after publishing to avoid concurrency conflicts.</param>
        public static void Publish<TPayload>(this IPublishable messageBus, string topic, TPayload payload)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            var envelope = messageBus.EnvelopeFactory.Create(topic, payload);
            messageBus.PublishEnvelope(envelope);
        }

        /// <summary>
        /// Publish the given payload as an event on the default topic
        /// </summary>
        /// <typeparam name="TPayload">The type of payload object</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="payload">The payload object to publish as an event. This object should not
        /// be modified after publishing, to prevent concurrency conflicts.</param>
        public static void Publish<TPayload>(this IPublishable messageBus, TPayload payload)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Publish(messageBus, string.Empty, payload);
        }

        /// <summary>
        /// Publish the given payload as an event on the topic.
        /// This overload is used when the object type is not known at compile time but is only
        /// available at runtime.
        /// </summary>
        /// <param name="messageBus">The message bus</param>
        /// <param name="topic">The name of the channel</param>
        /// <param name="payloadType">The runtime-known type of the payload object</param>
        /// <param name="payload">The payload object itself.</param>
        public static void Publish(this IPublishable messageBus, string topic, Type payloadType, object payload)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            var factoryMethod = messageBus.EnvelopeFactory.GetType()
                .GetMethod(nameof(messageBus.EnvelopeFactory.Create))
                .MakeGenericMethod(payloadType);
            var envelope = factoryMethod.Invoke(messageBus.EnvelopeFactory, new[] { new[] { topic }, payload, null });

            var method = messageBus.GetType().GetMethod(nameof(messageBus.PublishEnvelope)).MakeGenericMethod(payloadType);
            method.Invoke(messageBus, new[] { envelope });
        }

        /// <summary>
        /// Publish an encapsulated message to the bus
        /// </summary>
        /// <param name="messageBus">The message bus</param>
        /// <param name="message">The encapsulated message with all necessary publish details</param>
        public static void PublishMessage(this IPublishable messageBus, IPublishableMessage message)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(message, nameof(message));
            message.PublishTo(messageBus);
        }

        /// <summary>
        /// Build a subscription using common options
        /// </summary>
        /// <typeparam name="TPayload">The type of object to listen for</typeparam>
        /// <param name="messageBus">The message bus</param>
        /// <param name="build">Lambda function to setup the subscription builder.</param>
        /// <returns>The subscription token which, when disposed, cancels the subscription.</returns>
        public static IDisposable Subscribe<TPayload>(this IPubSubBus messageBus, Action<ITopicSubscriptionBuilder<TPayload>> build)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(build, nameof(build));

            var builder = new SubscriptionBuilder<TPayload>(messageBus, messageBus.WorkerPool);
            build(builder);
            var subscription = builder.BuildSubscription();

            var token = messageBus.Subscribe(builder.Topics, subscription);
            return builder.WrapToken(token);
        }

        /// <summary>
        /// Convenience method to subscribe to a single topic
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="topic"></param>
        /// <param name="subscription"></param>
        /// <returns>The subscription token which, when disposed, cancels the subscription</returns>
        public static IDisposable Subscribe<TPayload>(this IPubSubBus messageBus, string topic, ISubscription<TPayload> subscription)
        {
            return messageBus.Subscribe(new[] { topic ?? string.Empty }, subscription);
        }

        public static IDisposable AutoWireupSubscribers(this IPubSubBus messageBus, object obj, bool useWeakReference = false)
        {
            var tokens = new SubscriptionScanner(messageBus, messageBus.Logger).DetectAndWireUpAll(obj, useWeakReference);
            return new DisposableCollection(tokens);
        }

        public static IDisposable SubscribeUntyped(this IPubSubBus messageBus, Type payloadType, string[] topics, Action act, bool useWeakReference = false)
        {
            return new UntypedSubscriptionBuilder(messageBus).SubscribeUntyped(payloadType, topics, act, useWeakReference);
        }

        public static IDisposable SubscribeUntyped(this IPubSubBus messageBus, Type payloadType, string[] topics, object target, MethodInfo subscriber, bool useWeakReference = false)
        {
            return new UntypedSubscriptionBuilder(messageBus).SubscribeUntyped(payloadType, topics, target, subscriber, useWeakReference);
        }

        public static IDisposable SubscribeEnvelopeUntyped(this IPubSubBus messageBus, Type payloadType, string[] topics, object target, MethodInfo subscriber, bool useWeakReference = false)
        {
            return new UntypedSubscriptionBuilder(messageBus).SubscribeEnvelopeUntyped(payloadType, topics, target, subscriber, useWeakReference);
        }

        public static WrappedAction<TPayload> WrapAction<TPayload>(this IPubSubBus messageBus, Action<TPayload> action, Action<IThreadSubscriptionBuilder<TPayload>> build = null)
        {
            return new ActionWrapper<TPayload>().WrapAction(messageBus, action, build);
        }
    }
}