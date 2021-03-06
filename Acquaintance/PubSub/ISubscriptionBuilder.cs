﻿using System;

namespace Acquaintance.PubSub
{
    public interface ISubscriptionBuilderBase
    {
        /// <summary>
        /// The message bus used by the subscription builder.
        /// </summary>
        IPubSubBus MessageBus { get; }
    }

    public interface ITopicSubscriptionBuilder<TPayload> : ISubscriptionBuilderBase
    {
        /// <summary>
        /// Use the given channel name
        /// </summary>
        /// <param name="topics">The name of the channel</param>
        /// <returns>The builder</returns>
        IActionSubscriptionBuilder<TPayload> WithTopic(params string[] topics);

        /// <summary>
        /// Use the default channel
        /// </summary>
        /// <returns>The builder</returns>
        IActionSubscriptionBuilder<TPayload> WithDefaultTopic();

        /// <summary>
        /// The subscription should be invoked for all topics
        /// </summary>
        /// <returns></returns>
        IActionSubscriptionBuilder<TPayload> ForAllTopics();
    }

    public interface IActionSubscriptionBuilder<TPayload> : ISubscriptionBuilderBase
    {
        /// <summary>
        /// Invoke the given callback in response to the event. The callback receives the raw message payload
        /// </summary>
        /// <param name="action">The callback action to invoke</param>
        /// <param name="useWeakReferences">Whether to use a weak reference when storing the callback reference</param>
        /// <returns>The builder</returns>
        IThreadSubscriptionBuilder<TPayload> Invoke(Action<TPayload> action, bool useWeakReferences = false);

        /// <summary>
        /// Invoke the given callback in response to the event. The callback receives the message envelope
        /// </summary>
        /// <param name="action"></param>
        /// <param name="useWeakReferences"></param>
        /// <returns></returns>
        IThreadSubscriptionBuilder<TPayload> InvokeEnvelope(Action<Envelope<TPayload>> action, bool useWeakReferences = false);

        /// <summary>
        /// Invoke a method on a handler object in response to the event
        /// </summary>
        /// <param name="handler">The handler object which responds to the event</param>
        /// <returns>The builder</returns>
        IThreadSubscriptionBuilder<TPayload> Invoke(ISubscriptionHandler<TPayload> handler);

        /// <summary>
        /// Instantiate a service and use that service to handle the event
        /// </summary>
        /// <typeparam name="TService">The type of service to instantiate</typeparam>
        /// <param name="createService">A factory method to create the service</param>
        /// <param name="handler">The callback to invoke with the service and the payload</param>
        /// <param name="cacheService">True to create a single service instance and reuse it, false to create a new instance every time</param>
        /// <returns>The builder</returns>
        IThreadSubscriptionBuilder<TPayload> ActivateAndInvoke<TService>(Func<TPayload, TService> createService, Action<TService, TPayload> handler, bool cacheService = true)
            where TService : class;

        /// <summary>
        /// Transform the event payload to a new type, and re-publish on the new channel
        /// </summary>
        /// <typeparam name="TOutput">The type to transform the payload to</typeparam>
        /// <param name="transform">The callback to transform the data from the input to the output type</param>
        /// <param name="newTopic">The new channel name to publish the transformed message to</param>
        /// <returns>The builder</returns>
        IThreadSubscriptionBuilder<TPayload> TransformTo<TOutput>(Func<TPayload, TOutput> transform, string newTopic = null);

        /// <summary>
        /// Specify a custom subscription to be the final destination of the message. The provided subscription
        /// should handle it's own dispatch, options will not be provided to select which thread to dispatch on
        /// </summary>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        IDetailsSubscriptionBuilder<TPayload> UseCustomSubscriber(ISubscription<TPayload> subscriber);

        /// <summary>
        /// Specify a custom subscriber reference to be the final destination of the message
        /// </summary>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        IThreadSubscriptionBuilder<TPayload> UseCustomSubscriber(ISubscriberReference<TPayload> subscriber);
    }

    public interface IThreadSubscriptionBuilder<TPayload> : ISubscriptionBuilderBase
    {
        /// <summary>
        /// Execute the subscriber on a managed worker thread
        /// </summary>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> OnWorker();

        /// <summary>
        /// Execute the subscriber on the thread where the payload is published. This turns the
        /// pub/sub operation into a blocking operation and recurses on the stack. It should not
        /// be used in most cases.
        /// </summary>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> Immediate();

        /// <summary>
        /// Execute the subscriber on the thread with the given thread id
        /// </summary>
        /// <param name="threadId"></param>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> OnThread(int threadId);

        /// <summary>
        /// Execute the subscriber on the .NET threadpool
        /// </summary>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> OnThreadPool();

        /// <summary>
        /// Create a new dedicated worker thread for the subscription and execute the subscription
        /// exclusively on that thread.
        /// </summary>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> OnDedicatedWorker();
    }

    public interface IDetailsSubscriptionBuilder<TPayload> : ISubscriptionBuilderBase
    {
        /// <summary>
        /// Use a filter to determine if the message should be sent to this subscription or not.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> WithFilter(Func<TPayload, bool> filter);

        /// <summary>
        /// Use a filter on the envelope to determine if the message should be sent to this subscription or not.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        IDetailsSubscriptionBuilder<TPayload> WithFilterEnvelope(Func<Envelope<TPayload>, bool> filter);

        /// <summary>
        /// Handle a maximum number of events on this channel before automatically unsubscribing
        /// </summary>
        /// <param name="maxEvents"></param>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> MaximumEvents(int maxEvents);

        /// <summary>
        /// Modify the ISubscription before it is added to the message bus. Allows the use of custom
        /// options and wrappers/decorators which are not part of the builder
        /// </summary>
        /// <param name="wrap">A callback function to modify the generated ISubscription</param>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> WrapSubscription(Func<ISubscription<TPayload>, ISubscription<TPayload>> wrap);

        /// <summary>
        /// Wrap the base subscription, before the rest of the pipeline is constructed
        /// </summary>
        /// <param name="wrap"></param>
        /// <returns></returns>
        IDetailsSubscriptionBuilder<TPayload> WrapSubscriptionBase(Func<ISubscription<TPayload>, ISubscription<TPayload>> wrap);
    }
}
