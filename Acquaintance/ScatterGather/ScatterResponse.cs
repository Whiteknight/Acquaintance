using System;

namespace Acquaintance.ScatterGather
{
    public class ScatterResponse<TResponse>
    {
        public static ScatterResponse<TResponse> Success(Guid participantId, string name, TResponse value)
        {
            return new ScatterResponse<TResponse>
            {
                ParticipantId = participantId,
                Name = name,
                Value = value,
                IsSuccess = true
            };
        }

        public static ScatterResponse<TResponse> Error(Guid participantId, string name, Exception error)
        {
            return new ScatterResponse<TResponse>
            {
                ParticipantId = participantId,
                Name = name,
                IsSuccess = false,
                ErrorInformation = error
            };
        }

        public static ScatterResponse<TResponse> NoResponse(Guid participantId, string name)
        {
            return new ScatterResponse<TResponse>
            {
                ParticipantId = participantId,
                Name = name,
                IsSuccess = true,
                IsEmpty = true
            };
        }

        private ScatterResponse()
        {

        }

        public TResponse Value { get; private set; }
        public Guid ParticipantId { get; private set; }
        public bool IsSuccess { get; private set; }
        public Exception ErrorInformation { get; private set; }
        public string Name { get; private set; }
        public bool IsEmpty { get; private set; }

        public void ThrowExceptionIfPresent()
        {
            if (ErrorInformation != null)
                throw ErrorInformation;
        }
    }
}