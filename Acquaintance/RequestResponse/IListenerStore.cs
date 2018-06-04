using System;

namespace Acquaintance.RequestResponse
{
    /// <summary>
    /// Storage for listeners
    /// </summary>
    public interface IListenerStore
    {
        /// <summary>
        /// Adds a listener to respond on the specified channel
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="topic"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
        IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener);

        /// <summary>
        /// Get a listener for the specified channel
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="topic"></param>
        /// <returns>A listener or null if none are configured</returns>
        IListener<TRequest, TResponse> GetListener<TRequest, TResponse>(string topic);

        /// <summary>
        /// Remove the listener from the channel
        /// </summary>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="topic"></param>
        /// <param name="listener"></param>
        void RemoveListener<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener);
    }
}