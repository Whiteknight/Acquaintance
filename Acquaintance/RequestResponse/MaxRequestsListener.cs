﻿using System.Threading;

namespace Acquaintance.RequestResponse
{
    public class MaxRequestsListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListener<TRequest, TResponse> _inner;
        private int _maxRequests;

        public MaxRequestsListener(IListener<TRequest, TResponse> inner, int maxRequests)
        {
            _inner = inner;
            _maxRequests = maxRequests;
        }

        public bool CanHandle(TRequest request)
        {
            return _maxRequests > 0 || _inner.CanHandle(request);
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            if (ShouldStopListening)
                return new ImmediateResponse<TResponse>(null);
            var maxRequests = Interlocked.Decrement(ref _maxRequests);
            if (maxRequests >= 0)
                return _inner.Request(request);

            ShouldStopListening = true;
            return new ImmediateResponse<TResponse>(null);
        }

        public bool ShouldStopListening { get; private set; }
    }
}
