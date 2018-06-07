using System.Threading;

namespace Acquaintance.Threading
{
    /// <summary>
    /// Used the the IntervalWorkerThread, specifies the behavior to perform at each interval
    /// </summary>
    public interface IIntervalWorkStrategy
    {
        /// <summary>
        /// Create a context object to hold the necessary work state
        /// </summary>
        /// <returns></returns>
        IIntervalWorkerContext CreateContext();

        /// <summary>
        /// Do the work 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tokenSource"></param>
        void DoWork(IIntervalWorkerContext context, CancellationTokenSource tokenSource);
    }
}