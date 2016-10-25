using System;

namespace Acquaintance.RequestResponse
{
    public class ImmediateReqResSubscription<TRequest, TResponse> : IReqResSubscription<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly Func<TRequest, TResponse> _request;
        private readonly Func<TRequest, bool> _filter;

        public ImmediateReqResSubscription(Func<TRequest, TResponse> request, Func<TRequest, bool> filter)
        {
            _request = request;
            _filter = filter;
        }

        public bool CanHandle(TRequest request)
        {
            return _filter == null || _filter(request);
        }

        public TResponse Request(TRequest request)
        {
            return _request(request);
        }
    }
}