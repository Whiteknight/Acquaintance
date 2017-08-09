using System;
using System.Threading;

namespace Acquaintance.RequestResponse
{
    public class CircuitBreakerListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListener<TRequest, TResponse> _inner;
        private readonly int _breakMs;
        private readonly int _maxFailedRequests;

        private int _failedRequests;
        private DateTime _restartTime;        

        public CircuitBreakerListener(IListener<TRequest, TResponse> inner, int maxFailedRequests, int breakMs)
        {
            _inner = inner;
            _failedRequests = 0;
            _breakMs = breakMs;
            _maxFailedRequests = maxFailedRequests;
        }

        public Guid Id
        {
            get { return _inner.Id; }
            set { _inner.Id = value; }
        }

        public bool ShouldStopListening => _inner.ShouldStopListening;

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _inner.CanHandle(request);
        }

        public IDispatchableRequest<TResponse> Request(Envelope<TRequest> request)
        {
            // TODO: Move breaking logic into a reusable CircuitBreaker class so we can share the
            // core logic with Scatter/Gather
            if (_failedRequests >= _maxFailedRequests)
            {
                if (DateTime.Now >= _restartTime)
                    _failedRequests = 0;
                else
                    return ImmediateResponse<TResponse>.Error(Id, new Exception("Maximum number of attempts has been exceeded"));
            }

            var response = _inner.Request(request);
            if (response.Success)
                _failedRequests = 0;
            else
            {
                var failedRequests = Interlocked.Increment(ref _failedRequests);
                if (failedRequests >= _maxFailedRequests)
                    _restartTime = DateTime.Now.AddMilliseconds(_breakMs);
            }
            return response;
        }
    }
}
