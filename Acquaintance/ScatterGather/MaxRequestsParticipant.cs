using System;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class MaxRequestsParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipant<TRequest, TResponse> _inner;
        private int _maxRequests;

        public MaxRequestsParticipant(IParticipant<TRequest, TResponse> inner, int maxRequests)
        {
            Assert.IsInRange(maxRequests, nameof(maxRequests), 1, int.MaxValue);
            _inner = inner;
            _maxRequests = maxRequests;
        }

        public bool ShouldStopParticipating { get; private set; }

        public Guid Id
        {
            get { return _inner.Id; }
            set { _inner.Id = value; }
        }

        public bool CanHandle(TRequest request)
        {
            return _maxRequests > 0 || _inner.CanHandle(request);
        }

        public void Scatter(TRequest request, ScatterRequest<TResponse> scatter)
        {
            if (ShouldStopParticipating)
                return;

            var maxRequests = Interlocked.Decrement(ref _maxRequests);
            if (maxRequests < 0)
            {
                ShouldStopParticipating = true;
                return;
            }

            _inner.Scatter(request, scatter);

            if (maxRequests == 0)
                ShouldStopParticipating = true;
        }
    }
}
