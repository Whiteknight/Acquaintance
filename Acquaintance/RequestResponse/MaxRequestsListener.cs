using System;
using System.Threading;

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

        public bool ShouldStopListening { get; private set; }

        public Guid Id
        {
            get { return _inner.Id; }
            set { _inner.Id = value; }
        }

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _maxRequests > 0 || _inner.CanHandle(request);
        }

        public void Request(Envelope<TRequest> envelope, IResponseReceiver<TResponse> request)
        {
            if (ShouldStopListening)
            {
                request.SetNoResponse();
                return;
            }
            var maxRequests = Interlocked.Decrement(ref _maxRequests);
            if (maxRequests >= 0)
            {
                _inner.Request(envelope, request);
                return;
            }

            ShouldStopListening = true;
            request.SetNoResponse();
        }
    }
}
