using System;
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
            {
                Interlocked.Exchange(ref _failedRequests, 0);
                Interlocked.MemoryBarrier();
                return true;
            }
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
            if (failedRequests >= _maxFailedRequests)
            {
                var newRestartTime = DateTime.UtcNow.AddMilliseconds(_breakMs).Ticks;
                Interlocked.CompareExchange(ref _restartTime, newRestartTime, restartTime);
            }
            Interlocked.MemoryBarrier();
        }
    }

    public class WindowedCountingCircuitBreaker : ICircuitBreaker
    {
        private readonly int _breakMs;
        private readonly int _maxFailedRequests;
        private readonly int _windowSize;
        private readonly bool[] _events;
        private int _currentIndex;
        private long _restartTime;
        private int _errorCount;

        public WindowedCountingCircuitBreaker(int breakMs, int maxFailedRequests, int windowSize)
        {
            _breakMs = breakMs;
            _maxFailedRequests = maxFailedRequests;
            _windowSize = windowSize;
            _events = new bool[windowSize];
            for (int i = 0; i < windowSize; i++)
                _events[i] = true;
            _currentIndex = -1;
            _errorCount = 0;
        }

        public bool CanProceed()
        {
            var errorCount = Interlocked.CompareExchange(ref _errorCount, 0, 0);
            if (errorCount < _maxFailedRequests)
                return true;
            var restartTime = Interlocked.Read(ref _restartTime);
            if (DateTime.UtcNow.Ticks >= restartTime)
            {
                Interlocked.Exchange(ref _errorCount, 0);
                return true;
            }
            return false;
        }

        public void RecordResult(bool success)
        {
            var index = Interlocked.Increment(ref _currentIndex) % _windowSize;
            var restartTime = Interlocked.Read(ref _restartTime);

            // TODO: this isn't safe. If we have a small windowSize and a large number of concurrent calls,
            // it's possible that two threads could be accessing this slot at the same time.
            var currentValue = _events[index];
            _events[index] = success;

            int delta = 0;
            // If we're removing an error from the end of the queue, decrement the error count
            if (!currentValue)
                delta -= 1;

            // If we're adding a new error to the start of the queue, increment the error count
            if (!success)
                delta += 1;

            var errorCount = Interlocked.Add(ref _errorCount, delta);
            if (errorCount >= _maxFailedRequests)
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
