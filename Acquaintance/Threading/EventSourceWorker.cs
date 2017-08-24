using Acquaintance.Sources;
using System;
using System.Threading;
using Acquaintance.Utility;

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
            _thread.Start();
        }

        public Guid Id { get; }

        public int ThreadId => _thread.ManagedThreadId;

        public void Stop()
        {
            _tokenSource.Cancel();
            if (!_thread.Join(TimeSpan.FromSeconds(5)))
                _thread.Abort();
            _thread.Join();
        }

        public void Dispose()
        {
            _thread.Abort();
            _thread.Join();
        }

        private void ThreadFunction()
        {
            var token = _tokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                _source.CheckForEvents(_context, token);
                if (_context.IsComplete)
                {
                    // TODO: Some kind of alert or event that tells the rest of the system that we've stopped?
                    return;
                }
                if (_context.IterationDelayMs < 0)
                    _context.IterationDelayMs = DefaultIterationDelayMs;
                if (_context.IterationDelayMs > 0)
                    Thread.Sleep(_context.IterationDelayMs);
            }
        }
    }
}