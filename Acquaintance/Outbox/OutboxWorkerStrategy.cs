using System.Threading;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class OutboxWorkerStrategy : IIntervalWorkStrategy
    {
        private readonly OutboxManager _manager;
        private readonly int _pollDelayMs;

        public OutboxWorkerStrategy(OutboxManager manager, int pollDelayMs)
        {
            Assert.ArgumentNotNull(manager, nameof(manager));
            Assert.IsInRange(pollDelayMs, nameof(pollDelayMs), 1000, int.MaxValue);

            _manager = manager;
            _pollDelayMs = pollDelayMs;
        }

        public void DoWork(IIntervalWorkerContext context, CancellationTokenSource tokenSource)
        {
            _manager.TryFlushAll(tokenSource);
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