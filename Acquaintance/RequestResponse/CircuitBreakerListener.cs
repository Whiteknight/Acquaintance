using System;
using Acquaintance.Common;

namespace Acquaintance.RequestResponse
{
    public class CircuitBreakerListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListener<TRequest, TResponse> _inner;
        private readonly CircuitBreaker _circuitBreaker;

        public CircuitBreakerListener(IListener<TRequest, TResponse> inner, int maxFailedRequests, int breakMs)
        {
            _inner = inner;
            _circuitBreaker = new CircuitBreaker(breakMs, maxFailedRequests);
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
            if (!_circuitBreaker.CanProceed())
                return ImmediateResponse<TResponse>.Error(Id, new Exception("Maximum number of attempts has been exceeded"));
            
            var response = _inner.Request(request);
            _circuitBreaker.RecordResult(response.Success);
            return response;
        }
    }
}
