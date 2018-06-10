using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Acquaintance.Utility
{
    /// <summary>
    /// Error-handling tool which breaks a connection when a sufficient number of errors have been accumulated
    /// </summary>
    public interface ICircuitBreaker
    {
        /// <summary>
        /// Determines if the operation can proceed
        /// </summary>
        /// <returns>true if the operation can proceed, false otherwise</returns>
        bool CanProceed();

        /// <summary>
        /// Record a result.
        /// </summary>
        /// <param name="success">If true, the breaker is considered healthy. If false, the breaker may trip.</param>
        void RecordResult(bool success);
    }

    public class SequentialCountingCircuitBreaker : ICircuitBreaker
    {
        private readonly int _breakMs;
        private readonly int _maxFailedRequests;

        private int _failedRequests;
        private long _restartTime;

        public SequentialCountingCircuitBreaker(int breakMs, int maxFailedRequests)
        {
            _breakMs = breakMs;
            _maxFailedRequests = maxFailedRequests;
            _failedRequests = 0;
        }

        public bool CanProceed()
        {
            var failedRequests = Interlocked.CompareExchange(ref _failedRequests, 0, 0);
            if (failedRequests < _maxFailedRequests)
                return true;
            var restartTime = Interlocked.Read(ref _restartTime);
            if (DateTime.UtcNow.Ticks >= restartTime)
                return true;
            return false;
        }

        public void RecordResult(bool success)
        {
            if (success)
            {
                Interlocked.Exchange(ref _failedRequests, 0);
                Interlocked.MemoryBarrier();
                return;
            }

            var restartTime = Interlocked.Read(ref _restartTime);

            var failedRequests = Interlocked.Increment(ref _failedRequests);
            if (failedRequests >= _maxFailedRequests && restartTime <= DateTime.UtcNow.Ticks)
            {
                var newRestartTime = DateTime.UtcNow.AddMilliseconds(_breakMs).Ticks;
                Interlocked.CompareExchange(ref _restartTime, newRestartTime, restartTime);
            }
            Interlocked.MemoryBarrier();
        }
    }

    // TODO: This class is experimental, harden it up
    public class WindowedCountingCircuitBreaker : ICircuitBreaker
    {
        private readonly int _breakMs;
        private readonly int _maxFailedRequests;
        private readonly int _windowSize;
        private readonly ConcurrentQueue<bool> _events;
        private long _restartTime;
        private int _errorCount;

        public WindowedCountingCircuitBreaker(int breakMs, int maxFailedRequests, int windowSize)
        {
            _breakMs = breakMs;
            _maxFailedRequests = maxFailedRequests;
            _windowSize = windowSize;
            _events = new ConcurrentQueue<bool>();
            for (int i = 0; i < _windowSize; i++)
                _events.Enqueue(true);
            _errorCount = 0;
        }

        public bool CanProceed()
        {
            var errorCount = Interlocked.CompareExchange(ref _errorCount, 0, 0);
            if (errorCount < _maxFailedRequests)
                return true;
            var restartTime = Interlocked.Read(ref _restartTime);
            if (DateTime.UtcNow.Ticks >= restartTime)
                return true;
            return false;
        }

        public void RecordResult(bool success)
        {
            _events.Enqueue(success);
            if (!_events.TryDequeue(out bool currentValue))
                return;

            int delta = 0;
            // If we're removing an error from the end of the queue, decrement the error count
            if (!currentValue)
                delta -= 1;

            // If we're adding a new error to the start of the queue, increment the error count
            if (!success)
                delta += 1;

            var restartTime = Interlocked.Read(ref _restartTime);
            var errorCount = Interlocked.Add(ref _errorCount, delta);
            if (errorCount >= _maxFailedRequests && restartTime <= DateTime.UtcNow.Ticks)
            {
                var newRestartTime = DateTime.UtcNow.AddMilliseconds(_breakMs).Ticks;
                Interlocked.CompareExchange(ref _restartTime, newRestartTime, restartTime);
            }
        }
    }

    // TODO: Mode where we count the number of failures in the last unit of time
    //public class WindowedTimeCircuitBreaker : ICircuitBreaker
    //{

    //}
}
