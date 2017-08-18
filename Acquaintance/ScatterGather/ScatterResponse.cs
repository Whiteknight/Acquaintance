using System;

namespace Acquaintance.ScatterGather
{
    public class ScatterResponse<TResponse>
    {
        public ScatterResponse(TResponse response, Guid participantId, Exception errorInformation)
        {
            Response = response;
            ParticipantId = participantId;
            ErrorInformation = errorInformation;
            Success = errorInformation == null;
        }

        public TResponse Response { get; private set; }
        public Guid ParticipantId { get; }
        public bool Success { get; private set; }
        public Exception ErrorInformation { get; }
        public bool Completed { get; set; }

        public void ThrowExceptionIfPresent()
        {
            if (ErrorInformation != null)
                throw ErrorInformation;
        }
    }
}