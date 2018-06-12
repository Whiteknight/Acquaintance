using System;
using System.Collections.Concurrent;
using System.Threading;
using Acquaintance.Logging;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class OutboxMonitor : IOutboxMonitor, IDisposable
    {
        private readonly IntervalWorkerThread _thread;
        private readonly IDisposable _threadToken;
        private readonly ConcurrentDictionary<long, IOutboxSender> _outboxes;

        private long _outboxId;

        public OutboxMonitor(ILogger logger, IWorkerPool workers, int pollDelayMs)
        {
            Assert.ArgumentNotNull(logger, nameof(logger));
            Assert.ArgumentNotNull(workers, nameof(workers));
            Assert.IsInRange(pollDelayMs, nameof(pollDelayMs), 1000, int.MaxValue);

            _outboxes = new ConcurrentDictionary<long, IOutboxSender>();
            var strategy = new OutboxWorkerStrategy(this, pollDelayMs);
            _thread = new IntervalWorkerThread(logger, strategy);
            _threadToken = workers.RegisterManagedThread("Outbox Module", _thread.ThreadId, "Outbox worker thread");
        }

        public void Start()
        {
            _thread.Start();
        }

        public void Stop()
        {
            _thread.Stop();
        }

        public void Dispose()
        {
            _thread?.Dispose();
            _threadToken?.Dispose();
        }

        public IDisposable AddOutboxToBeMonitored(IOutboxSender outbox)
        {
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            // TODO: Need to check to ensure we don't contain this outbox already?
            var id = Interlocked.Increment(ref _outboxId);
            if (!_outboxes.TryAdd(id, outbox))
                throw new Exception("Cannot add outbox to be monitored");
            return new Token(this, id);
        }

        public void TryFlushAll(CancellationTokenSource tokenSource)
        {
            Assert.ArgumentNotNull(tokenSource, nameof(tokenSource));

            var token = tokenSource.Token;
            var entries = _outboxes.ToArray();
            foreach (var entry in entries)
            {
                // TODO: Should we do anything with the return value?
                entry.Value.TrySend();
                if (token.IsCancellationRequested)
                    return;
            }
        }

        private void RemoveOutbox(long id)
        {
            _outboxes.TryRemove(id);
        }

        private class Token : DisposeOnceToken
        {
            private readonly OutboxMonitor _monitor;
            private readonly long _id;

            public Token(OutboxMonitor monitor, long id)
            {
                _monitor = monitor;
                _id = id;
            }

            protected override void Dispose(bool disposing)
            {
                _monitor.RemoveOutbox(_id);
            }

            public override string ToString()
            {
                return $"Monitor outbox Id={_id}";
            }
        }
    }
}