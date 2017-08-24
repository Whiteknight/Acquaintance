using System;

namespace Acquaintance.ScatterGather
{
    public class ScatterResponse<TResponse>
    {
        public ScatterResponse(TResponse value, Guid participantId, Exception errorInformation)
        {
            Value = value;
            ParticipantId = participantId;
            ErrorInformation = errorInformation;
            Success = errorInformation == null;
        }

        public ScatterResponse(Guid participantId)
        {
            ParticipantId = participantId;
            IsEmpty = true;
            Success = true;
        }

        public TResponse Value { get; private set; }
        public Guid ParticipantId { get; }
        public bool Success { get; private set; }
        public Exception ErrorInformation { get; }
        public bool IsEmpty { get; private set; }

        public void ThrowExceptionIfPresent()
        {
            if (ErrorInformation != null)
                throw ErrorInformation;
        }
    }
}