using Acquaintance.PubSub;
using System;
using System.Reflection;
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
            var envelope = factoryMethod.Invoke(messageBus.EnvelopeFactory, new[] { topic, payload, null });

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

            var token = messageBus.Subscribe(builder.Topic, subscription);
            return builder.WrapToken(token);
        }

        public static IDisposable AutoSubscribe(this IPubSubBus messageBus, object obj)
        {
            return new SubscriptionScanner().AutoSubscribe(messageBus, obj);
        }

        public static IDisposable SubscribeUntyped(this IPubSubBus messageBus, Type payloadType, string[] topics, object target, MethodInfo subscriber)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(payloadType, nameof(payloadType));
            Assert.ArgumentNotNull(target, nameof(target));
            Assert.ArgumentNotNull(subscriber, nameof(subscriber));

            var method = typeof(PubSubMessageBusExtensions).GetMethod(nameof(SubscribeUntypedInternal), BindingFlags.Static | BindingFlags.NonPublic);
            method = method.MakeGenericMethod(payloadType);
            return method.Invoke(null, new[] { messageBus, topics, target, subscriber } ) as IDisposable;
        }

        private static IDisposable SubscribeUntypedInternal<TPayload>(IPubSubBus messageBus, string[] topics, object target, MethodInfo subscriber)
        {
            if (topics == null || topics.Length == 0)
            {
                return Subscribe<TPayload>(messageBus, b => b
                    .WithDefaultTopic()
                    .Invoke(p => subscriber.Invoke(target, new object[] { p })));
            }

            var tokens = new DisposableCollection();
            foreach (var topic in topics)
            {
                var token = Subscribe<TPayload>(messageBus, b => b
                    .WithTopic(topic)
                    .Invoke(p => subscriber.Invoke(target, new object[] { p })));
                tokens.Add(token);
            }
            return tokens;
        }

        public static IDisposable SubscribeEnvelopeUntyped(this IPubSubBus messageBus, Type payloadType, string[] topics, object target, MethodInfo subscriber)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(payloadType, nameof(payloadType));
            Assert.ArgumentNotNull(target, nameof(target));
            Assert.ArgumentNotNull(subscriber, nameof(subscriber));

            var method = typeof(PubSubMessageBusExtensions).GetMethod(nameof(SubscribeEnvelopeUntypedInternal), BindingFlags.Static | BindingFlags.NonPublic);
            method = method.MakeGenericMethod(payloadType);
            return method.Invoke(null, new[] { messageBus, topics, target, subscriber }) as IDisposable;
        }

        private static IDisposable SubscribeEnvelopeUntypedInternal<TPayload>(IPubSubBus messageBus, string[] topics, object target, MethodInfo subscriber)
        {
            if (topics == null || topics.Length == 0)
            {
                return Subscribe<TPayload>(messageBus, b => b
                    .WithDefaultTopic()
                    .InvokeEnvelope(e => subscriber.Invoke(target, new object[] { e })));
            }

            var tokens = new DisposableCollection();
            foreach (var topic in topics)
            {
                var token = Subscribe<TPayload>(messageBus, b => b
                    .WithTopic(topic)
                    .InvokeEnvelope(e => subscriber.Invoke(target, new object[] { e })));
                tokens.Add(token);
            }
            return tokens;
        }

        public static WrappedAction<TPayload> WrapAction<TPayload>(this IPubSubBus messageBus, Action<TPayload> action, Action<IThreadSubscriptionBuilder<TPayload>> build = null)
        {
            return new ActionWrapper<TPayload>().WrapAction(messageBus, action, build);
        }
    }
}