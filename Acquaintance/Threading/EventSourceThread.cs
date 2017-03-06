using Acquaintance.Sources;
using System;
using System.Threading;

namespace Acquaintance.Threading
{
    public class EventSourceThread : IEventSourceThread
    {
        private const int IterationDelayMs = 1000;
        private readonly IEventSource _source;
        private readonly Thread _thread;
        private readonly IEventSourceContext _context;
        private readonly CancellationTokenSource _tokenSource;

        public EventSourceThread(IEventSource source, IEventSourceContext context)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

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
            bool ok = _thread.Join(TimeSpan.FromSeconds(5));
            if (!ok)
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
                // TODO: Make IterationDelayMs configurable when we set up the source. 
                Thread.Sleep(IterationDelayMs);
            }
        }
    }
}