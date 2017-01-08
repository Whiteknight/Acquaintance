using System;

namespace Acquaintance.ScatterGather
{
    public class FilteredParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipant<TRequest, TResponse> _inner;
        private readonly Func<TRequest, bool> _filter;

        public FilteredParticipant(IParticipant<TRequest, TResponse> inner, Func<TRequest, bool> filter)
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

        public bool ShouldStopParticipating => _inner.ShouldStopParticipating;
    }
}