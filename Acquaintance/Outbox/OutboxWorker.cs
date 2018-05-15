using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class OutboxWorker : IDisposable
    {
        private readonly ConcurrentDictionary<long, IOutbox> _outboxes;
        private readonly int _pollDelayMs;
        
        private readonly Thread _thread;
        
        private readonly CancellationTokenSource _shouldStop;

        public OutboxWorker(ConcurrentDictionary<long, IOutbox> outboxes, int pollDelayMs)
        {
            Assert.IsInRange(pollDelayMs, nameof(pollDelayMs), 1000, int.MaxValue);
            _outboxes = outboxes;
            _pollDelayMs = pollDelayMs;
            
            _thread = new Thread(ThreadFunction);
            _shouldStop = new CancellationTokenSource();
        }

        public int ThreadId => _thread.ManagedThreadId;

        public void Start()
        {
            _thread.Start();
        }

        public void Stop()
        {
            if (_shouldStop.IsCancellationRequested)
                return;
            _shouldStop.Cancel();
            if (_thread.ThreadState == ThreadState.Unstarted || _thread.ThreadState == ThreadState.StopRequested || _thread.ThreadState == ThreadState.Stopped)
                return;
            _thread.Join();
        }

        private void ThreadFunction()
        {
            var token = _shouldStop.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Task.Delay(_pollDelayMs, token).Wait(token);
                }
                catch
                {
                    return;
                }

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
        }

        public void Dispose()
        {
            Stop();
        }
    }
}