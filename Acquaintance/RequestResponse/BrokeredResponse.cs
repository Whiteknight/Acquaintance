using System.Collections.Generic;

namespace Acquaintance.RequestResponse
{
    public class BrokeredResponse<TResponse> : IBrokeredResponse<TResponse>
    {
        public BrokeredResponse(IReadOnlyList<TResponse> responses)
        {
            Responses = responses;
        }

        public IReadOnlyList<TResponse> Responses { get; private set; }
    }
}