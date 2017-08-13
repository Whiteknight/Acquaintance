using System;
using System.Threading;

namespace Acquaintance.Common
{
    public class CircuitBreaker
    {
        private readonly int _breakMs;
        private readonly int _maxFailedRequests;

        private int _failedRequests;
        private DateTime _restartTime;

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
            if (DateTime.Now >= _restartTime)
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
                _restartTime = DateTime.Now.AddMilliseconds(_breakMs);
        }
    }
}
