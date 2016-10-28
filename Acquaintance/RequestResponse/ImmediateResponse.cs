namespace Acquaintance.RequestResponse
{
    public class ImmediateResponse<TResponse> : IDispatchableRequest<TResponse>
    {
        public ImmediateResponse(TResponse response)
        {
            Response = response;
        }

        public void Dispose()
        {
        }

        public TResponse Response { get; }

        public bool WaitForResponse()
        {
            return true;
        }
    }
}