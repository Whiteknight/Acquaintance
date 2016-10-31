using System.Collections;
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

        public IEnumerator<TResponse> GetEnumerator()
        {
            return Responses.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Responses.GetEnumerator();
        }
    }
}