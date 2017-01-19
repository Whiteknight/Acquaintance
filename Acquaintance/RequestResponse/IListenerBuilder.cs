using System;

namespace Acquaintance.RequestResponse
{
    /// <summary>
    /// Setup the channel that the Listener listens on
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IChannelListenerBuilder<TRequest, TResponse>
    {
        /// <summary>
        /// Listen on the specific channel
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IActionListenerBuilder<TRequest, TResponse> WithChannelName(string name);

        /// <summary>
        /// Listen on the default channel
        /// </summary>
        /// <returns></returns>
        IActionListenerBuilder<TRequest, TResponse> OnDefaultChannel();
    }

    /// <summary>
    /// Setup the action the Listener should take
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IActionListenerBuilder<TRequest, TResponse>
    {
        /// <summary>
        /// Invoke a callback function
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="useWeakReference">Whether to use a weak reference or not. Weak references
        /// do not keep the subscription alive if the subscriber has been garbage collected</param>
        /// <returns></returns>
        IThreadListenerBuilder<TRequest, TResponse> Invoke(Func<TRequest, TResponse> listener, bool useWeakReference = false);

        /// <summary>
        /// Route the request to a different channel based on routing rules
        /// </summary>
        /// <param name="build"></param>
        /// <returns></returns>
        IThreadListenerBuilder<TRequest, TResponse> Route(Action<RouteBuilder<TRequest, TResponse>> build);

        /// <summary>
        /// Transform the request payload to a new object type and forward it to a new channel
        /// </summary>
        /// <typeparam name="TTransformed"></typeparam>
        /// <param name="sourceChannelName"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        IThreadListenerBuilder<TRequest, TResponse> TransformRequestTo<TTransformed>(string sourceChannelName, Func<TRequest, TTransformed> transform);

        /// <summary>
        /// Transform the response payload from the source type of the expected response type
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="sourceChannelName"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        IThreadListenerBuilder<TRequest, TResponse> TransformResponseFrom<TSource>(string sourceChannelName, Func<TSource, TResponse> transform);
    }

    /// <summary>
    /// Setup the threading dispatch rules for the listener
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IThreadListenerBuilder<TRequest, TResponse>
    {
        /// <summary>
        /// Execute the request immediately on the caller thread
        /// </summary>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> Immediate();

        /// <summary>
        /// Create a dedicated worker thread and dispatch all requests to that thread
        /// </summary>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> OnDedicatedThread();

        /// <summary>
        /// Dispatch all requests to the thread with the given thread ID
        /// </summary>
        /// <param name="threadId"></param>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> OnThread(int threadId);

        /// <summary>
        /// Dispatch the request to the .NET thread pool
        /// </summary>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> OnThreadPool();

        /// <summary>
        /// Dispatch the request to the Acquaintance managed thread pool
        /// </summary>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> OnWorkerThread();
    }

    /// <summary>
    /// Setup additional details about the listener
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface IDetailsListenerBuilder<TRequest, TResponse>
    {
        /// <summary>
        /// Set a maximum number of requests, after which the Listener stops listening
        /// </summary>
        /// <param name="maxRequests"></param>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> MaximumRequests(int maxRequests);

        /// <summary>
        /// Specify a filter for the listener. Only requests which satisfy the filter condition
        /// will be sent to the listener
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> WithFilter(Func<TRequest, bool> filter);

        /// <summary>
        /// Specify a timeout for the request
        /// </summary>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> WithTimeout(int timeoutMs);

        /// <summary>
        /// Setup a circuit breaker, which will break the channel after a number of consecutive
        /// failed requests. A broken channel allows time for the Listener to recover without
        /// stacking up additional requests.
        /// </summary>
        /// <param name="maxAttempts"></param>
        /// <param name="breakMs"></param>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> WithCircuitBreaker(int maxAttempts, int breakMs);

        /// <summary>
        /// Make custom modifications to the Listener before it is installed on the channel
        /// </summary>
        /// <param name="modify"></param>
        /// <returns></returns>
        IDetailsListenerBuilder<TRequest, TResponse> ModifyListener(Func<IListener<TRequest, TResponse>, IListener<TRequest, TResponse>> modify);
    }
}
