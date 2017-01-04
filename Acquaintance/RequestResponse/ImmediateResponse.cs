namespace Acquaintance.RequestResponse
{
    public class ImmediateResponse<TResponse> : IDispatchableRequest<TResponse>
    {
        public ImmediateResponse(TResponse[] responses)
        {
            Responses = responses ?? new TResponse[0];
        }

        public void Dispose()
        {
        }

        public TResponse[] Responses { get; }

        public bool WaitForResponse()
        {
            return true;
        }
    }
}