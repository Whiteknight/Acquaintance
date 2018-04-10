using System;
using System.Threading;

namespace Acquaintance.Utility
{
    // TODO: Mode where we count the number of failures in the last N requests
    // TODO: Mode where we count the number of failures in the last unit of time
    public class CircuitBreaker
    {
        private readonly int _breakMs;
        private readonly int _maxFailedRequests;

        private volatile int _failedRequests;
        private long _restartTime;

        public CircuitBreaker(int breakMs, int maxFailedRequests)
        {
            _breakMs = breakMs;
            _maxFailedRequests = maxFailedRequests;
            _failedRequests = 0;
        }

        public bool CanProceed()
        {
            if (_failedRequests < _maxFailedRequests)
                return true;
            var restartTime = Interlocked.Read(ref _restartTime);
            if (DateTime.UtcNow.Ticks >= restartTime)
            {
                _failedRequests = 0;
                return true;
            }
            return false;
        }

        public void RecordResult(bool success)
        {
            if (success)
            {
                _failedRequests = 0;
                return;
            }

            var failedRequests = Interlocked.Increment(ref _failedRequests);
            if (failedRequests >= _maxFailedRequests)
                _restartTime = DateTime.UtcNow.AddMilliseconds(_breakMs).Ticks;
        }
    }
}
