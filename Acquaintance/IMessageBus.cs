﻿using Acquaintance.Modules;
using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;
using Acquaintance.Logging;
using Acquaintance.Routing;

namespace Acquaintance
{
    public interface IBusBase
    {
        /// <summary>
        /// Extension modules for the message bus which may add additional features.
        /// </summary>
        IModuleManager Modules { get; }

        /// <summary>
        /// The threadpool which holds worker threads for dispatching requests and events
        /// </summary>
        IWorkerPool WorkerPool { get; }

        /// <summary>
        /// Factory for creating envelopes
        /// </summary>
        IEnvelopeFactory EnvelopeFactory { get; }

        /// <summary>
        /// Source for all logging operations.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// The ID of the bus. The ID should be unique among federated MessageBus instances, but can be any
        /// arbitrary string in the absence of federation
        /// </summary>
        string Id { get; }
    }

    public interface IPublishable : IBusBase
    {
        /// <summary>
        /// Publish a message envelope to the bus
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="envelope"></param>
        void PublishEnvelope<TPayload>(Envelope<TPayload> envelope);
    }

    public interface IPubSubBus : IPublishable
    {
        /// <summary>
        /// Subscribe to pub/sub events for the given type, on the given topic.
        /// </summary>
        /// <typeparam name="TPayload">The type of event payload to subscribe to</typeparam>
        /// <param name="topics">If null, subscribes to all topics. Otherwise subscribes to the list of topics provided</param>
        /// <param name="subscription">The subscription object to receive the events</param>
        /// <returns>A disposable token which represents the subscription. Dispose this to cancel the subscription.</returns>
        IDisposable Subscribe<TPayload>(string[] topics, ISubscription<TPayload> subscription);

        /// <summary>
        /// Router for publish topics
        /// </summary>
        IPublishTopicRouter PublishRouter { get; }
    }

    public interface IReqResBus : IBusBase
    {
        /// <summary>
        /// Make a request and expect a single response
        /// </summary>
        /// <typeparam name="TRequest">The type of request object</typeparam>
        /// <typeparam name="TResponse">The type of response object</typeparam>
        /// <param name="envelope">The request object which represents the input arguments to the RPC call</param>
        /// <returns>A disposable token which represents the subscription. Dispose this to cancel the subscription.</returns>
        IRequest<TResponse> RequestEnvelope<TRequest, TResponse>(Envelope<TRequest> envelope);

        /// <summary>
        /// Listen for an incoming request and provide a response.
        /// </summary>
        /// <typeparam name="TRequest">The type of request object</typeparam>
        /// <typeparam name="TResponse">The type of response object</typeparam>
        /// <param name="topic">The name of the channel</param>
        /// <param name="listener">The listener to receive the request and provide a response</param>
        /// <returns>A disposable token which represents the subscription. Dispose this to cancel the subscription.</returns>
        IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener);

        /// <summary>
        /// Router for request topics
        /// </summary>
        IRequestTopicRouter RequestRouter { get; }
    }

    public interface IScatterGatherBus : IBusBase
    {
        /// <summary>s
        /// Make a request and receive many responses
        /// </summary>
        /// <typeparam name="TRequest">The type of request object</typeparam>
        /// <typeparam name="TResponse">The type of response object</typeparam>
        /// <param name="envelope">The request object</param>
        /// <returns>A disposable token which represents the subscription. Dispose this to cancel the subscription.</returns>
        IScatter<TResponse> ScatterEnvelope<TRequest, TResponse>(Envelope<TRequest> envelope);

        /// <summary>
        /// Listen for incoming scatters and provide responses
        /// </summary>
        /// <typeparam name="TRequest">The type of request object</typeparam>
        /// <typeparam name="TResponse">The type of response object</typeparam>
        /// <param name="topic">The name of the channel</param>
        /// <param name="participant">The participant which receives the request and provides responses.</param>
        /// <returns>A disposable token which represents the subscription. Dispose this to cancel the subscription.</returns>
        IDisposable Participate<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant);

        /// <summary>
        /// Router for scatter topics
        /// </summary>
        IScatterTopicRouter ScatterRouter { get; }
    }

    public interface IMessageBus : IPubSubBus, IReqResBus, IScatterGatherBus, IDisposable
    {
        /// <summary>
        /// Get an EventLoop for the current thread. The event loop cannot be used on any other thread
        /// </summary>
        /// <returns></returns>
        IEventLoop GetEventLoop();
    }
}
