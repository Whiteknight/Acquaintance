using System;

namespace Acquaintance.RequestResponse
{
    public class ImmediateResponse<TResponse> : IDispatchableRequest<TResponse>
    {
        public ImmediateResponse(Guid listenerId, TResponse response)
        {
            Response = response;
            ListenerId = listenerId;
        }

        public TResponse Response { get; }
        public bool Success => true;
        public Exception ErrorInformation => null;

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