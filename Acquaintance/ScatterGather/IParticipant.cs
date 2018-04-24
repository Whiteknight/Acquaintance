using System;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// Participant type, which receives scatters and returns responses
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IParticipant<TRequest, TResponse>
    {
        /// <summary>
        /// Determine if the participant can handle the request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        bool CanHandle(Envelope<TRequest> request);

        /// <summary>
        /// Receive the request and produce responses
        /// </summary>
        /// <param name="request"></param>
        /// <param name="scatter"></param>
        /// <returns></returns>
        void Scatter(Envelope<TRequest> request, IGatherReceiver<TResponse> scatter);

        /// <summary>
        /// Returns true if the participant should stop participating
        /// </summary>
        bool ShouldStopParticipating { get; }

        /// <summary>
        /// The unique ID of the participant
        /// </summary>
        Guid Id { get; set; }
        string Name { get; }
    }
}