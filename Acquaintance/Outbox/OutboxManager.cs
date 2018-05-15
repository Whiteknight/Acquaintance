using System;
using System.Collections.Concurrent;
using System.Threading;
using Acquaintance.Threading;

namespace Acquaintance.Outbox
{
    public class OutboxManager : IOutboxManager, IDisposable
    {
        private readonly OutboxWorker _thread;
        private readonly IDisposable _threadToken;
        private readonly ConcurrentDictionary<long, IOutbox> _outboxes;

        private long _outboxId;

        public OutboxManager(IWorkerPool workers, int pollDelayMs)
        {
            _outboxes = new ConcurrentDictionary<long, IOutbox>();
            _thread = new OutboxWorker(_outboxes, pollDelayMs);
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

        public IDisposable AddOutboxToBeMonitored(IOutbox outbox)
        {
            var id = Interlocked.Increment(ref _outboxId);
            bool ok = _outboxes.TryAdd(id, outbox);
            if (!ok)
                throw new Exception("Cannot add outbox to be monitored");
            return new Token(this, id);
        }

        private void RemoveOutbox(long id)
        {
            _outboxes.TryRemove(id, out IOutbox outbox);
        }

        private class Token : IDisposable
        {
            private readonly OutboxManager _manager;
            private readonly long _id;

            public Token(OutboxManager manager, long id)
            {
                _manager = manager;
                _id = id;
            }

            public void Dispose()
            {
                _manager.RemoveOutbox(_id);
            }
        }
    }
}