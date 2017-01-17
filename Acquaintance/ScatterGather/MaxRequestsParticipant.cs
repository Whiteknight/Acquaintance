using System;
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

        public Guid Id
        {
            get { return _inner.Id; }
            set { _inner.Id = value; }
        }

        public bool CanHandle(TRequest request)
        {
            return _maxRequests > 0 || _inner.CanHandle(request);
        }

        public IDispatchableScatter<TResponse> Scatter(TRequest request)
        {
            if (ShouldStopParticipating)
                return new ImmediateGather<TResponse>(Id, null);
            var maxRequests = Interlocked.Decrement(ref _maxRequests);
            if (maxRequests >= 0)
                return _inner.Scatter(request);

            ShouldStopParticipating = true;
            return new ImmediateGather<TResponse>(Id, null);
        }

        public bool ShouldStopParticipating { get; private set; }
    }
}
