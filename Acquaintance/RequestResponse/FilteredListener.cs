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

        public bool CanHandle(TRequest request)
        {
            return _inner.CanHandle(request) || _filter(request);
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            return _inner.Request(request);
        }
    }
}