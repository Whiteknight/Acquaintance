using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// A pending scatter/gather operation. Allows the monitoring and control of
    /// asynchronous responses
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    public interface IScatter<TResponse> : IDisposable
    {
        /// <summary>
        /// Get the next response, waiting up to a certain timeout
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        ScatterResponse<TResponse> GetNextResponse(TimeSpan timeout);

        /// <summary>
        /// Get the next response, waiting up to a certain timeout
        /// </summary>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        ScatterResponse<TResponse> GetNextResponse(int timeoutMs);

        /// <summary>
        /// Get the next response, waiting up to a default timeout
        /// </summary>
        /// <returns></returns>
        ScatterResponse<TResponse> GetNextResponse();

        /// <summary>
        /// Gather a number of responses, waiting up to a certain timeout
        /// </summary>
        /// <param name="max"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        IReadOnlyList<ScatterResponse<TResponse>> GatherResponses(int max, TimeSpan timeout);

        /// <summary>
        /// Gather a number of responses, waiting up to a default timeout
        /// </summary>
        /// <param name="max"></param>
        /// <returns></returns>
        IReadOnlyList<ScatterResponse<TResponse>> GatherResponses(int max);

        /// <summary>
        /// Attempt to gather all responses, waiting up to a certain timeout
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        IReadOnlyList<ScatterResponse<TResponse>> GatherResponses(TimeSpan timeout);

        /// <summary>
        /// Attempt to gather all responses, waiting up to a default timeout
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<ScatterResponse<TResponse>> GatherResponses();

        /// <summary>
        /// The total number of participants in this scatter
        /// </summary>
        int TotalParticipants { get; }

        /// <summary>
        /// The number of participants which have provided a result.
        /// Note that because of the asynchronous nature of this operation, this count
        /// might be incremented before the response is available to be read.
        /// </summary>
        int CompletedParticipants { get; }
    }
}