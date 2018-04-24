using Acquaintance.Threading;
using System;

namespace Acquaintance.ScatterGather
{
    public class DispatchableScatter<TRequest, TResponse> : IThreadAction, IDispatchableScatter
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly Envelope<TRequest> _request;
        private readonly string _name;
        private readonly IGatherReceiver<TResponse> _scatter;

        public DispatchableScatter(IParticipantReference<TRequest, TResponse> func, Envelope<TRequest> request, Guid participantId, string name, IGatherReceiver<TResponse> scatter)
        {
            _func = func;
            _request = request;
            _name = name;
            _scatter = scatter;
            ParticipantId = participantId;
        }

        public Guid ParticipantId { get; }

        public void Execute()
        {
            try
            {
                var response = _func.Invoke(_request);
                _scatter.AddResponse(ParticipantId, ScatterResponse<TResponse>.Success(ParticipantId, _name, response));
            }
            catch (Exception e)
            {
                _scatter.AddResponse(ParticipantId, ScatterResponse<TResponse>.Error(ParticipantId, _name, e));
            }
        }
    }
}