using Acquaintance.RequestResponse;
using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public class ImmediateParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;

        public ImmediateParticipant(IParticipantReference<TRequest, TResponse> func)
        {
            _func = func;
        }

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            var value = _func.Invoke(request);
            return new ImmediateResponse<TResponse>(value);
        }

        public static IParticipant<TRequest, TResponse> Create(Func<TRequest, IEnumerable<TResponse>> func)
        {
            return new ImmediateParticipant<TRequest, TResponse>(new StrongParticipantReference<TRequest, TResponse>(func));
        }

        public bool ShouldStopParticipating => !_func.IsAlive;
    }
}