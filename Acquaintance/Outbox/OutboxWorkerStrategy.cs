using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class OutboxWorkerStrategy : IIntervalWorkStrategy
    {
        private readonly ConcurrentDictionary<long, IOutbox> _outboxes;
        private readonly int _pollDelayMs;

        public OutboxWorkerStrategy(ConcurrentDictionary<long, IOutbox> outboxes, int pollDelayMs)
        {
            Assert.IsInRange(pollDelayMs, nameof(pollDelayMs), 1000, int.MaxValue);
            _outboxes = outboxes;
            _pollDelayMs = pollDelayMs;
        }

        public void DoWork(IIntervalWorkerContext context, CancellationTokenSource tokenSource)
        {
            var token = tokenSource.Token;
            var keys = _outboxes.Keys.ToArray();
            foreach (var key in keys)
            {
                var exists = _outboxes.TryGetValue(key, out IOutbox outbox);
                if (!exists)
                    continue;
                outbox.TryFlush();
                if (token.IsCancellationRequested)
                    return;
            }
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