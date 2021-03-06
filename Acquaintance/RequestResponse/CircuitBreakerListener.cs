﻿using System;
using Acquaintance.Utility;

namespace Acquaintance.RequestResponse
{
    public class CircuitBreakerListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListener<TRequest, TResponse> _inner;
        private readonly ICircuitBreaker _circuitBreaker;

        public CircuitBreakerListener(IListener<TRequest, TResponse> inner, int breakMs, int maxFailures)
        {
            Assert.ArgumentNotNull(inner, nameof(inner));
            _inner = inner;
            _circuitBreaker = new SequentialCountingCircuitBreaker(breakMs, maxFailures);
        }

        public CircuitBreakerListener(IListener<TRequest, TResponse> inner, ICircuitBreaker circuitBreaker)
        {
            Assert.ArgumentNotNull(inner, nameof(inner));
            Assert.ArgumentNotNull(circuitBreaker, nameof(circuitBreaker));
            _inner = inner;
            _circuitBreaker = circuitBreaker;
        }

        public bool ShouldStopListening => _inner.ShouldStopListening;

        public Guid Id
        {
            get => _inner.Id;
            set => _inner.Id = value;
        }

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _inner.CanHandle(request) && _circuitBreaker.CanProceed();
        }

        public void Request(Envelope<TRequest> envelope, IResponseReceiver<TResponse> request)
        {
            if (!_circuitBreaker.CanProceed())
            {
                request.SetNoResponse();
                return;
            }
            request = new Receiver(_circuitBreaker, request);
            _inner.Request(envelope, request);
        }

        private class Receiver : IResponseReceiver<TResponse>
        {
            private readonly ICircuitBreaker _circuitBreaker;
            private readonly IResponseReceiver<TResponse> _inner;

            public Receiver(ICircuitBreaker circuitBreaker, IResponseReceiver<TResponse> inner)
            {
                _circuitBreaker = circuitBreaker;
                _inner = inner;
            }

            public void SetNoResponse()
            {
                _inner.SetNoResponse();
            }

            public void SetResponse(TResponse response)
            {
                _circuitBreaker.RecordResult(true);
                _inner.SetResponse(response);
            }

            public void SetError(Exception e)
            {
                _circuitBreaker.RecordResult(false);
                _inner.SetError(e);
            }
        }
    }
}
