using Acquaintance.Modules;
using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using System;

namespace Acquaintance
{
    public interface IBusBase
    {
        /// <summary>
        /// The threadpool which holds worker threads for dispatching requests and events
        /// </summary>
        IThreadPool ThreadPool { get; }
        IEnvelopeFactory EnvelopeFactory { get; }
    }

    public interface IPubSubBus : IBusBase
    {
        /// <summary>
        /// Subscribe to pub/sub events for the given type, on the given channel name.
        /// </summary>
        /// <typeparam name="TPayload">The type of event payload to subscribe to</typeparam>
        /// <param name="topic">The name of the channel</param>
        /// <param name="subscription">The subscription object to receive the events</param>
        /// <returns>A disposable token which represents the subscription. Dispose this to cancel the subscription.</returns>
        IDisposable Subscribe<TPayload>(string topic, ISubscription<TPayload> subscription);

        void PublishEnvelope<TPayload>(Envelope<TPayload> envelope);
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
    }

    public interface IScatterGatherBus : IBusBase
    {
        /// <summary>s
        /// Make a request and receive many responses
        /// </summary>
        /// <typeparam name="TRequest">The type of request object</typeparam>
        /// <typeparam name="TResponse">The type of response object</typeparam>
        /// <param name="topic">The name of the channel</param>
        /// <param name="request">The request object</param>
        /// <returns>A disposable token which represents the subscription. Dispose this to cancel the subscription.</returns>
        IScatter<TResponse> Scatter<TRequest, TResponse>(string topic, TRequest request);

        /// <summary>
        /// Listen for incoming scatters and provide responses
        /// </summary>
        /// <typeparam name="TRequest">The type of request object</typeparam>
        /// <typeparam name="TResponse">The type of response object</typeparam>
        /// <param name="topic">The name of the channel</param>
        /// <param name="participant">The participant which receives the request and provides responses.</param>
        /// <returns>A disposable token which represents the subscription. Dispose this to cancel the subscription.</returns>
        IDisposable Participate<TRequest, TResponse>(string topic, IParticipant<TRequest, TResponse> participant);

        // TODO: An Eavesdrop variant for scatter/gather?
    }

    public interface IMessageBus : IPubSubBus, IReqResBus, IScatterGatherBus, IDisposable
    {
        /// <summary>
        /// Extension modules for the message bus which may add additional features.
        /// </summary>
        IModuleManager Modules { get; }

        /// <summary>
        /// Run an event loop in the current thread to process messages queued against the current
        /// thread ID
        /// </summary>
        /// <param name="shouldStop">A callback which, if provided, will allow the runloop to 
        /// terminate when a condition is satisfied.</param>
        /// <param name="timeoutMs">A maximum amount of time to wait before the shouldStop
        /// condition is tested.</param>
        void RunEventLoop(Func<bool> shouldStop = null, int timeoutMs = 500);

        /// <summary>
        /// Runs an event loop in the current thread to process messages queued agains the current
        /// thread ID. Continue looping until the queue is empty or until a maximum number of
        /// events are processed, and then return.
        /// </summary>
        /// <param name="max">The maximum number of events to process before returning.</param>
        void EmptyActionQueue(int max);
    }
}
