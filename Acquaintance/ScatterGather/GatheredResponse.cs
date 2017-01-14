using System.Collections;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public class GatheredResponse<TResponse> : IGatheredResponse<TResponse>
    {
        public GatheredResponse(IReadOnlyList<TResponse> responses)
        {
            Responses = responses;
        }

        public IReadOnlyList<TResponse> Responses { get; }

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