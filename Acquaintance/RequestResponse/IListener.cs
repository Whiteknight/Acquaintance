using System;

namespace Acquaintance.RequestResponse
{
    /// <summary>
    /// A listener receives incoming requests and produces a response
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IListener<TRequest, TResponse>
    {
        /// <summary>
        /// Determine if the listener can handle the request depending on the current state of the
        /// listener and any predicates or preconditions.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        bool CanHandle(Envelope<TRequest> request);

        /// <summary>
        /// Receive the request and produce a response. This behavior may be dispatched to a worker
        /// thread, so the return value is a waiter that can be used to wait for the result to be
        /// ready.
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        void Request(Envelope<TRequest> envelope, Request<TResponse> request);

        /// <summary>
        /// Whether the listener has satisfied all the requests it is able, and needs to be removed
        /// from the channel to avoid further requests
        /// </summary>
        bool ShouldStopListening { get; }

        /// <summary>
        /// The unique ID of the listener
        /// </summary>
        Guid Id { get; set; }
    }
}