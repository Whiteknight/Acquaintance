using System;
using System.Threading;
using System.Threading.Tasks;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.Threading
{
    public sealed class IntervalWorkerThread : IDisposable
    {
        private const int DefaultIterationDelayMs = 1000;

        private readonly ILogger _log;
        private readonly IIntervalWorkStrategy _strategy;
        private readonly Thread _thread;
        private readonly CancellationTokenSource _shouldStop;

        public IntervalWorkerThread(ILogger log, IIntervalWorkStrategy strategy)
        {
            _log = log;
            _strategy = strategy;
            _thread = new Thread(ThreadFunction);
            _shouldStop = new CancellationTokenSource();
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public int ThreadId => _thread.ManagedThreadId;

        public void Start()
        {
            if (_thread.ThreadState == ThreadState.Unstarted || _thread.ThreadState == ThreadState.Stopped)
                _thread.Start();
        }

        public void Stop()
        {
            if (_shouldStop.IsCancellationRequested)
                return;
            _shouldStop.Cancel();
            if (_thread.ThreadState == ThreadState.Unstarted || _thread.ThreadState == ThreadState.StopRequested || _thread.ThreadState == ThreadState.Stopped)
                return;
            if (!_thread.Join(TimeSpan.FromSeconds(5)))
                _log.Warn("Could not join thread " + _thread.ManagedThreadId);
        }

        public void Dispose()
        {
            Stop();
        }

        // TODO: Second "dynamic" mode of operation where we measure the amount of time spent doing work, and adjust the delay period accordingly
        private void ThreadFunction()
        {
            var context = _strategy.CreateContext();
            var token = _shouldStop.Token;
            while (!token.IsCancellationRequested)
            {
                ErrorHandling.IgnoreExceptions(() => _strategy.DoWork(context, _shouldStop), _log);
                if (context.IsComplete)
                    return;
                if (context.IterationDelayMs < 0)
                    context.IterationDelayMs = DefaultIterationDelayMs;
                if (context.IterationDelayMs > 0)
                    ErrorHandling.IgnoreExceptions(() => Task.Delay(context.IterationDelayMs, token).Wait(token));
            }
        }
    }
}
