using System.Threading;

namespace Acquaintance.Threading
{
    public interface IIntervalWorkStrategy
    {
        IIntervalWorkerContext CreateContext();

        void DoWork(IIntervalWorkerContext context, CancellationTokenSource tokenSource);
    }
}