using System.Collections.Generic;

namespace Acquaintance
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