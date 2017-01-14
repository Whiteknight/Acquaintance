namespace Acquaintance.ScatterGather
{
    public class ImmediateGather<TResponse> : IDispatchableScatter<TResponse>
    {
        public ImmediateGather(TResponse[] responses)
        {
            Responses = responses;
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