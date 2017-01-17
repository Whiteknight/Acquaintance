using System;

namespace Acquaintance.ScatterGather
{
    public class ImmediateGather<TResponse> : IDispatchableScatter<TResponse>
    {
        public ImmediateGather(Guid participantId, TResponse[] responses)
        {
            Responses = responses;
            ParticipantId = participantId;
        }

        public TResponse[] Responses { get; }
        public bool Success => true;
        public Exception ErrorInformation => null;

        public Guid ParticipantId { get; }

        public bool WaitForResponse()
        {
            return true;
        }

        public void Dispose()
        {
        }
    }
}