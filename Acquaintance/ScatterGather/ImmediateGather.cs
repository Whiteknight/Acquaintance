using System;

namespace Acquaintance.ScatterGather
{
    public class ImmediateGather<TResponse> : IDispatchableScatter<TResponse>
    {
        public ImmediateGather(Guid participantId, TResponse[] responses, bool success = true, Exception errorInformation = null)
        {
            Responses = responses ?? new TResponse[0];
            ParticipantId = participantId;
            Success = success;
            ErrorInformation = errorInformation;
        }

        public static ImmediateGather<TResponse> Error(Guid participantId, Exception errorInformation)
        {
            return new ImmediateGather<TResponse>(participantId, null, false, errorInformation);
        }

        public TResponse[] Responses { get; }
        public bool Success { get; }
        public Exception ErrorInformation { get; }


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