using System;
using System.Threading;

namespace Acquaintance.Utility
{
    public interface ICircuitBreaker
    {
        bool CanProceed();
        void RecordResult(bool success);
    }

    // TODO: Mode where we count the number of failures in the last N requests
    // TODO: Mode where we count the number of failures in the last unit of time
    public class SequentialCountingCircuitBreaker : ICircuitBreaker
    {
        private readonly int _breakMs;
        private readonly int _maxFailedRequests;

        private volatile int _failedRequests;
        private long _restartTime;

        public SequentialCountingCircuitBreaker(int breakMs, int maxFailedRequests)
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

    //public class WindowedCountingCircuitBreaker : ICircuitBreaker
    //{
    //    private readonly int _breakMs;
    //    private readonly int _maxFailedRequests;
    //    private readonly int _windowSize;

    //    public WindowedCountingCircuitBreaker(int breakMs, int maxFailedRequests, int windowSize)
    //    {
    //        _breakMs = breakMs;
    //        _maxFailedRequests = maxFailedRequests;
    //        _windowSize = windowSize;
    //    }

    //    public bool CanProceed()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void RecordResult(bool success)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class WindowedTimeCircuitBreaker : ICircuitBreaker
    //{

    //}
}
