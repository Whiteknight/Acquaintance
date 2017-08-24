using System;

namespace Acquaintance.RequestResponse
{
    public class FilteredListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListener<TRequest, TResponse> _inner;
        private readonly Func<TRequest, bool> _filter;

        public FilteredListener(IListener<TRequest, TResponse> inner, Func<TRequest, bool> filter)
        {
            _inner = inner;
            _filter = filter;
        }

        public Guid Id
        {
            get { return _inner.Id; }
            set { _inner.Id = value; }
        }

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _inner.CanHandle(request) && _filter(request.Payload);
        }

        public void Request(Envelope<TRequest> envelope, IResponseReceiver<TResponse> request)
        {
            _inner.Request(envelope, request);
        }

        public bool ShouldStopListening => _inner.ShouldStopListening;
    }
}