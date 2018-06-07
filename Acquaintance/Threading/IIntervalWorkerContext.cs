namespace Acquaintance.Threading
{
    /// <summary>
    /// Context object to hold state and status for the interval work strategy
    /// </summary>
    public interface IIntervalWorkerContext
    {
        /// <summary>
        /// Mark the work as being complete. Sets IsComplete to true and signals the interval worker thread
        /// to stop
        /// </summary>
        void Complete();

        /// <summary>
        /// True if the work is complete and the thread should stop. False otherwise.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// The duration to wait before invoking the work again
        /// </summary>
        int IterationDelayMs { get; set; }
    }

    public class IntervalWorkerContext : IIntervalWorkerContext
    {
        public void Complete()
        {
            IsComplete = true;
        }

        public bool IsComplete { get; private set; }
        public int IterationDelayMs { get; set; }
    }
}