using System;
using Acquaintance.Logging;

namespace Acquaintance.Threading
{
    public interface IWorkerContext : IActionDispatcher, IDisposable
    {
        /// <summary>
        /// The logger to use for this context
        /// </summary>
        ILogger Log { get; }

        /// <summary>
        /// True if the context has been requested to stop, false otherwise
        /// </summary>
        bool ShouldStop { get; }

        /// <summary>
        /// Sets ShouldStop to true and performs and other cleanup
        /// </summary>
        void Stop();

        /// <summary>
        /// Get the next queued action for the thread
        /// </summary>
        /// <param name="timeoutMs">if provided, specifies the timeout in milliseconds for trying to dequeue
        /// the next action</param>
        /// <returns></returns>
        IThreadAction GetAction(int? timeoutMs = null);
    }
}