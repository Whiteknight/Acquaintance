using Acquaintance.Threading;
using System;

namespace Acquaintance.ScatterGather
{
    public class DispatchableScatter<TRequest, TResponse> : IThreadAction, IDispatchableScatter
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly Envelope<TRequest> _request;
        private readonly IGatherReceiver<TResponse> _scatter;

        public DispatchableScatter(IParticipantReference<TRequest, TResponse> func, Envelope<TRequest> request, Guid participantId, IGatherReceiver<TResponse> scatter)
        {
            _func = func;
            _request = request;
            _scatter = scatter;
            ParticipantId = participantId;
        }

        public Guid ParticipantId { get; }

        public void Execute()
        {
            try
            {
                var response = _func.Invoke(_request);
                _scatter.AddResponse(ParticipantId, response);
            }
            catch (Exception e)
            {
                _scatter.AddError(ParticipantId, e);
            }
        }
    }
}