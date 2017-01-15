using System;

namespace Acquaintance.RequestResponse
{
    public class ImmediateResponse<TResponse> : IDispatchableRequest<TResponse>
    {
        public ImmediateResponse(TResponse response)
        {
            Response = response;
        }

        public TResponse Response { get; }
        public bool Success => true;
        public Exception ErrorInformation => null;

        public bool WaitForResponse()
        {
            return true;
        }

        public void Dispose()
        {
        }
    }
}