using Acquaintance.RequestResponse;
using System.Threading;

namespace Acquaintance.ScatterGather
{
    public class MaxRequestsParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipant<TRequest, TResponse> _inner;
        private int _maxRequests;

        public MaxRequestsParticipant(IParticipant<TRequest, TResponse> inner, int maxRequests)
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
            if (ShouldStopParticipating)
                return new ImmediateResponse<TResponse>(null);
            var maxRequests = Interlocked.Decrement(ref _maxRequests);
            if (maxRequests >= 0)
                return _inner.Request(request);

            ShouldStopParticipating = true;
            return new ImmediateResponse<TResponse>(null);
        }

        public bool ShouldStopParticipating { get; private set; }
    }
}
