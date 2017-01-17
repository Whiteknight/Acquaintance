using System;

namespace Acquaintance.RequestResponse
{
    public class ImmediateResponse<TResponse> : IDispatchableRequest<TResponse>
    {
        public ImmediateResponse(Guid listenerId, TResponse response, bool success = true, Exception errorInformation = null)
        {
            Response = response;
            ListenerId = listenerId;
            Success = success;
            ErrorInformation = errorInformation;
        }

        public static ImmediateResponse<TResponse> Error(Guid listenerId, Exception errorInformation)
        {
            return new ImmediateResponse<TResponse>(listenerId, default(TResponse), false, errorInformation);
        }

        public TResponse Response { get; }
        public bool Success { get; }
        public Exception ErrorInformation { get; }

        public Guid ListenerId { get; }

        public bool WaitForResponse()
        {
            return true;
        }

        public void Dispose()
        {
        }
    }
}