using System;

namespace Acquaintance.ScatterGather
{
    public class ImmediateGather<TResponse> : IDispatchableScatter<TResponse>
    {
        public ImmediateGather(TResponse[] responses)
        {
            Responses = responses;
        }

        public TResponse[] Responses { get; }
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