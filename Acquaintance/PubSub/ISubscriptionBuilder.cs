using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public interface IChannelSubscriptionBuilder<TPayload>
    {
        /// <summary>
        /// Use the given channel name
        /// </summary>
        /// <param name="channelName">The name of the channel</param>
        /// <returns>The builder</returns>
        IActionSubscriptionBuilder<TPayload> WithChannelName(string channelName);

        /// <summary>
        /// Use the default channel
        /// </summary>
        /// <returns>The builder</returns>
        IActionSubscriptionBuilder<TPayload> OnDefaultChannel();
    }

    public interface IActionSubscriptionBuilder<TPayload>
    {
        /// <summary>
        /// Invoke the given callback in response to the event
        /// </summary>
        /// <param name="action">The callback action to invoke</param>
        /// <param name="useWeakReferences">Whether to use a weak reference when storing the callback reference</param>
        /// <returns>The builder</returns>
        IThreadSubscriptionBuilder<TPayload> Invoke(Action<TPayload> action, bool useWeakReferences = true);

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
        /// <returns>The builder</returns>
        IThreadSubscriptionBuilder<TPayload> ActivateAndInvoke<TService>(Func<TPayload, TService> createService, Action<TService, TPayload> handler);

        /// <summary>
        /// Transform the event payload to a new type, and re-publish on the new channel
        /// </summary>
        /// <typeparam name="TOutput">The type to transform the payload to</typeparam>
        /// <param name="transform">The callback to transform the data from the input to the output type</param>
        /// <param name="newChannelName">The new channel name to publish the transformed message to</param>
        /// <returns>The builder</returns>
        IThreadSubscriptionBuilder<TPayload> TransformTo<TOutput>(Func<TPayload, TOutput> transform, string newChannelName = null);

        /// <summary>
        /// Route the message to a new channel based on rules
        /// </summary>
        /// <param name="build">A lambda function to prepare the route builder</param>
        /// <returns>The builder</returns>
        IThreadSubscriptionBuilder<TPayload> Route(Action<RouteBuilder<TPayload>> build);

        /// <summary>
        /// Distrubute the message to one of several channels using a round-robin dispatching scheme
        /// </summary>
        /// <param name="channels">A list of new channels to dispatch to</param>
        /// <returns>The builder</returns>
        IThreadSubscriptionBuilder<TPayload> Distribute(IEnumerable<string> channels);
    }

    public interface IThreadSubscriptionBuilder<TPayload>
    {
        /// <summary>
        /// Execute the subscriber on a managed worker thread
        /// </summary>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> OnWorkerThread();

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
        IDetailsSubscriptionBuilder<TPayload> OnDedicatedThread();
    }

    public interface IDetailsSubscriptionBuilder<TPayload>
    {
        /// <summary>
        /// Use a filter to determine if the message should be sent to this subscription or not.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>The builder</returns>
        IDetailsSubscriptionBuilder<TPayload> WithFilter(Func<TPayload, bool> filter);

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
        IDetailsSubscriptionBuilder<TPayload> ModifySubscription(Func<ISubscription<TPayload>, ISubscription<TPayload>> wrap);
    }
}
