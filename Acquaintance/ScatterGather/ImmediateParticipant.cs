using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// Executes the participant reference on the current thread
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public class ImmediateParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;

        public ImmediateParticipant(IParticipantReference<TRequest, TResponse> func)
        {
            _func = func;
        }

        public Guid Id { get; set; }
        public bool ShouldStopParticipating => !_func.IsAlive;

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public IDispatchableScatter<TResponse> Scatter(TRequest request)
        {
            var value = _func.Invoke(request);
            return new ImmediateGather<TResponse>(Id, value);
        }

        public static IParticipant<TRequest, TResponse> Create(Func<TRequest, IEnumerable<TResponse>> func)
        {
            return new ImmediateParticipant<TRequest, TResponse>(new StrongParticipantReference<TRequest, TResponse>(func));
        }
    }
}