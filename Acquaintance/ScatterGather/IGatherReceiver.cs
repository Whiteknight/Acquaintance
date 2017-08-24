using System;

namespace Acquaintance.ScatterGather
{
    public interface IGatherReceiver<in TResponse>
    {
        void AddResponse(Guid participantId, TResponse response);
        void AddError(Guid participantId, Exception error);
        void CompleteWithNoResponse(Guid participantId);
        void AddParticipant(Guid participantId);
    }
}