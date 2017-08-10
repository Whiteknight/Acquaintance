using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// Builder type to setup the Participant's channel
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface ITopicParticipantBuilder<TRequest, TResponse>
    {
        /// <summary>
        /// Specify a channel name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IActionParticipantBuilder<TRequest, TResponse> WithTopic(string name);

        /// <summary>
        /// Use the default (empty) channel
        /// </summary>
        /// <returns></returns>
        IActionParticipantBuilder<TRequest, TResponse> WithDefaultTopic();
    }

    /// <summary>
    /// Builder type to setup the Participant's action
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IActionParticipantBuilder<TRequest, TResponse>
    {
        /// <summary>
        /// Invoke a function and return the response
        /// </summary>
        /// <param name="participant"></param>
        /// <param name="useWeakReference"></param>
        /// <returns></returns>
        IThreadParticipantBuilder<TRequest, TResponse> Invoke(Func<TRequest, TResponse> participant, bool useWeakReference = false);

        /// <summary>
        /// Invoke a function and return the responses
        /// </summary>
        /// <param name="participant"></param>
        /// <param name="useWeakReference"></param>
        /// <returns></returns>
        IThreadParticipantBuilder<TRequest, TResponse> Invoke(Func<TRequest, IEnumerable<TResponse>> participant, bool useWeakReference = false);

        /// <summary>
        /// Specify routing rules 
        /// </summary>
        /// <param name="build"></param>
        /// <returns></returns>
        IThreadParticipantBuilder<TRequest, TResponse> Route(Action<RouteBuilder<TRequest, TResponse>> build);

        /// <summary>
        /// Transform the request payload to a new type, and re-scatter
        /// </summary>
        /// <typeparam name="TTransformed"></typeparam>
        /// <param name="sourceChannelName"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        IThreadParticipantBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceChannelName, Func<TRequest, TTransformed> transform);

        /// <summary>
        /// Transform the response value into a new and return it.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="sourceChannelName"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        IThreadParticipantBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceChannelName, Func<TSource, TResponse> transform);
    }

    /// <summary>
    /// Builder type to setup the Participant's thread affinity
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IThreadParticipantBuilder<TRequest, TResponse>
    {
        /// <summary>
        /// Execute immediately on the scatter thread
        /// </summary>
        /// <returns></returns>
        IDetailsParticipantBuilder<TRequest, TResponse> Immediate();

        /// <summary>
        /// Execute on a specific thread with the given thread ID
        /// </summary>
        /// <param name="threadId"></param>
        /// <returns></returns>
        IDetailsParticipantBuilder<TRequest, TResponse> OnThread(int threadId);

        /// <summary>
        /// Execute on the .NET threadpool
        /// </summary>
        /// <returns></returns>
        IDetailsParticipantBuilder<TRequest, TResponse> OnThreadPool();

        /// <summary>
        /// Execute on the Acquaintance threadpool
        /// </summary>
        /// <returns></returns>
        IDetailsParticipantBuilder<TRequest, TResponse> OnWorkerThread();

        /// <summary>
        /// Create a dedicated worker thread and execute the participant on it.
        /// </summary>
        /// <returns></returns>
        IDetailsParticipantBuilder<TRequest, TResponse> OnDedicatedThread();
    }

    /// <summary>
    /// Builder type to setup the Participant's details
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IDetailsParticipantBuilder<TRequest, TResponse>
    {
        /// <summary>
        /// Handle a maximum number of scatters and then stop participating.
        /// </summary>
        /// <param name="maxRequests"></param>
        /// <returns></returns>
        IDetailsParticipantBuilder<TRequest, TResponse> MaximumRequests(int maxRequests);

        /// <summary>
        /// Specify a filter to determine which scatters are responded to.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        IDetailsParticipantBuilder<TRequest, TResponse> WithFilter(Func<TRequest, bool> filter);

        /// <summary>
        /// Specify a maximum timeout before the sender stops waiting for the response
        /// </summary>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        IDetailsParticipantBuilder<TRequest, TResponse> WithTimeout(int timeoutMs);

        /// <summary>
        /// Make custom modifications to the IParticipant before it is added to the message bus
        /// </summary>
        /// <param name="modify"></param>
        /// <returns></returns>
        IDetailsParticipantBuilder<TRequest, TResponse> ModifyParticipant(Func<IParticipant<TRequest, TResponse>, IParticipant<TRequest, TResponse>> modify);
    }
}