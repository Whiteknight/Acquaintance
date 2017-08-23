using System;

namespace Acquaintance.RequestResponse
{
    /// <summary>
    /// An encapsulated request. Includes methods to wait for and retrieve results of an asynchronous listener
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    public interface IRequest<out TResponse>
    {
        /// <summary>
        /// Wait for the listener to complete. Returns true if the listener completed before the timeout,
        /// false if the timeout expired before a response is received
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        bool WaitForResponse(TimeSpan timeout);

        /// <summary>
        /// Wait for a response for a default amount of time (10 seconds)
        /// </summary>
        /// <returns></returns>
        bool WaitForResponse();

        /// <summary>
        /// Get the response. Returns a default value until the listener responds. Call WaitForResponse to
        /// wait for an asynchronous response
        /// </summary>
        /// <returns></returns>
        TResponse GetResponse();

        /// <summary>
        /// Get the response as an untyped object returns default(TResponse) until the listener responds
        /// </summary>
        /// <returns></returns>
        object GetResponseObject();

        /// <summary>
        /// Returns true if the listener has completed, including the case where the listener completes
        /// without setting a response value or an error.
        /// </summary>
        /// <returns></returns>
        bool IsComplete();

        /// <summary>
        /// Returns true if the listener has responded with a value or an error. Returns false if
        /// there was no listener or the listener has not responded
        /// </summary>
        /// <returns></returns>
        bool HasResponse();

        /// <summary>
        /// Gets Exception information if the listener throws. Returns null until the listener responds and
        /// returns null if the listener did not throw.
        /// </summary>
        /// <returns></returns>
        Exception GetErrorInformation();

        /// <summary>
        /// Throws the exception if there is one. Otherwise does nothing. 
        /// </summary>
        void ThrowExceptionIfError();
    }
}