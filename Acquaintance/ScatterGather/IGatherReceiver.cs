using System;

namespace Acquaintance.ScatterGather
{
    public interface IGatherReceiver<TResponse>
    {
        void AddResponse(Guid participantId, ScatterResponse<TResponse> response);
        void AddParticipant(Guid participantId);
    }
}