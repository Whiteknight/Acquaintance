using System;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// Participant wrapper/decorator type which passes on the request if a filter predicate is
    /// satisfied
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public class FilteredParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipant<TRequest, TResponse> _inner;
        private readonly Func<TRequest, bool> _filter;

        public FilteredParticipant(IParticipant<TRequest, TResponse> inner, Func<TRequest, bool> filter)
        {
            _inner = inner;
            _filter = filter;
        }

        public bool ShouldStopParticipating => _inner.ShouldStopParticipating;

        public Guid Id
        {
            get { return _inner.Id; }
            set { _inner.Id = value; }
        }

        public bool CanHandle(TRequest request)
        {
            return _inner.CanHandle(request) || _filter(request);
        }

        public IDispatchableScatter<TResponse> Scatter(TRequest request)
        {
            return _inner.Scatter(request);
        }
    }
}