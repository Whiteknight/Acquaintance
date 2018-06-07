namespace Acquaintance.Threading
{
    public enum DispatchThreadType
    {
        /// <summary>
        /// Dispatch the action to the best available vector
        /// </summary>
        NoPreference,

        /// <summary>
        /// Execute the action immediately on the current thread
        /// </summary>
        Immediate,

        /// <summary>
        /// Execute the action on a specific thread by thread ID
        /// </summary>
        SpecificThread,

        /// <summary>
        /// Execute the action on the first available worker thread
        /// </summary>
        AnyWorkerThread,

        /// <summary>
        /// Execute the action on the .NET thread pool
        /// </summary>
        ThreadpoolThread
    }
}