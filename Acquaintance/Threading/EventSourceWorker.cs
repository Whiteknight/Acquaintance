using Acquaintance.Sources;
using System;
using System.Threading;
using Acquaintance.Utility;
using System.Threading.Tasks;

namespace Acquaintance.Threading
{
    public class EventSourceWorker : IEventSourceWorker
    {
        private const int DefaultIterationDelayMs = 1000;

        private readonly IEventSource _source;
        private readonly Thread _thread;
        private readonly IEventSourceContext _context;
        private readonly CancellationTokenSource _tokenSource;

        public EventSourceWorker(IEventSource source, IEventSourceContext context)
        {
            Assert.ArgumentNotNull(source, nameof(source));
            Assert.ArgumentNotNull(context, nameof(context));

            _source = source;
            _context = context;

            Id = Guid.NewGuid();
            _tokenSource = new CancellationTokenSource();

            _thread = new Thread(ThreadFunction);
        }

        public Guid Id { get; }

        public int ThreadId => _thread.ManagedThreadId;

        public void Start()
        {
            if (_thread.ThreadState == ThreadState.Unstarted)
                _thread.Start();
        }

        public void Stop()
        {
            _tokenSource.Cancel();
            if (_thread.ThreadState == ThreadState.Unstarted || _thread.ThreadState == ThreadState.StopRequested || _thread.ThreadState == ThreadState.Stopped)
                return;
            if (!_thread.Join(TimeSpan.FromSeconds(5)))
            {
                // Log the error   
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void ThreadFunction()
        {
            var token = _tokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                _source.CheckForEvents(_context, token);
                if (_context.IsComplete)
                    return;
                if (_context.IterationDelayMs < 0)
                    _context.IterationDelayMs = DefaultIterationDelayMs;
                if (_context.IterationDelayMs > 0)
                {
                    try {
                        Task.Delay(_context.IterationDelayMs, token).Wait(token);
                    } catch {}
                }
            }
        }
    }
}