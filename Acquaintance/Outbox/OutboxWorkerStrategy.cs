using System.Threading;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class OutboxWorkerStrategy : IIntervalWorkStrategy
    {
        private readonly OutboxMonitor _monitor;
        private readonly int _pollDelayMs;

        public OutboxWorkerStrategy(OutboxMonitor monitor, int pollDelayMs)
        {
            Assert.ArgumentNotNull(monitor, nameof(monitor));
            Assert.IsInRange(pollDelayMs, nameof(pollDelayMs), 1000, int.MaxValue);

            _monitor = monitor;
            _pollDelayMs = pollDelayMs;
        }

        public void DoWork(IIntervalWorkerContext context, CancellationTokenSource tokenSource)
        {
            _monitor.TryFlushAll(tokenSource);
        }

        public IIntervalWorkerContext CreateContext()
        {
            return new IntervalWorkerContext
            {
                IterationDelayMs = _pollDelayMs
            };
        }
    }
}