using System;

namespace Acquaintance.Threading
{
    public interface IWorkerPool
    {
        /// <summary>
        /// Get the number of free worker threads currently running in the pool.
        /// </summary>
        int NumberOfRunningFreeWorkers { get; }

        /// <summary>
        /// Get a report of all threads running in the pool for debugging and auditing purposes
        /// </summary>
        /// <returns></returns>
        ThreadReport GetThreadReport();

        /// <summary>
        /// Start a new dedicated worker thread
        /// </summary>
        /// <returns></returns>
        WorkerToken StartDedicatedWorker();

        /// <summary>
        /// Stop a dedicated worker thread by thread ID
        /// </summary>
        /// <param name="threadId"></param>
        void StopDedicatedWorker(int threadId);

        /// <summary>
        /// Get the action dispatcher for the given thread ID
        /// </summary>
        /// <param name="threadId"></param>
        /// <param name="allowAutoCreate">true if the pool should automatically create a dispatcher if it 
        /// does not already exist</param>
        /// <returns></returns>
        IActionDispatcher GetDispatcher(int threadId, bool allowAutoCreate);

        /// <summary>
        /// Get the action dispatcher for the free workers
        /// </summary>
        /// <returns></returns>
        IActionDispatcher GetFreeWorkerDispatcher();

        /// <summary>
        /// Get the action dispatcher for the .NET thread pool
        /// </summary>
        /// <returns></returns>
        IActionDispatcher GetThreadPoolDispatcher();

        /// <summary>
        /// Get the first available action dispatcher
        /// </summary>
        /// <returns></returns>
        IActionDispatcher GetAnyWorkerDispatcher();

        /// <summary>
        /// Get the action dispatcher for the current thread
        /// </summary>
        /// <returns></returns>
        IActionDispatcher GetCurrentThreadDispatcher();

        /// <summary>
        /// Get the worker context for the current thread. This is used for examining thread state and
        /// running an event loop
        /// </summary>
        /// <returns></returns>
        IWorkerContext GetCurrentThreadContext();

        /// <summary>
        /// Register a thread with the system which has been allocated elsewhere. The worker pool will not
        /// actively manage the thread, but will include mention of it in the thread report. This is most
        /// commonly used for threads allocated in modules.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="threadId"></param>
        /// <param name="purpose"></param>
        /// <returns></returns>
        IDisposable RegisterManagedThread(string owner, int threadId, string purpose);
    }
}