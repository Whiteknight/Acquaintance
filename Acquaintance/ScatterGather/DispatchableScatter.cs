using Acquaintance.Threading;
using System;

namespace Acquaintance.ScatterGather
{
    public class DispatchableScatter<TRequest, TResponse> : IThreadAction, IDispatchableScatter
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly TRequest _request;
        private readonly ScatterRequest<TResponse> _scatter;

        public DispatchableScatter(IParticipantReference<TRequest, TResponse> func, TRequest request, Guid participantId, ScatterRequest<TResponse> scatter)
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
                var responses = _func.Invoke(_request);
                foreach (var response in responses)
                    _scatter.AddResponse(ParticipantId, response);
            }
            catch (Exception e)
            {
                _scatter.AddError(ParticipantId, e);
            }
            finally
            {
                _scatter.MarkParticipantComplete(ParticipantId);
            }
        }
    }
}