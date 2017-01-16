using Acquaintance.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.ScatterGather
{
    public class GatheredResponse<TResponse> : IGatheredResponse<TResponse>
    {
        public GatheredResponse(IReadOnlyList<CompleteGather<TResponse>> responses)
        {
            Responses = responses;
        }

        public IReadOnlyList<CompleteGather<TResponse>> Responses { get; }

        public void ThrowAnyExceptions()
        {
            var exceptions = Responses
                .Where(r => r != null)
                .Select(cr => cr.ErrorInformation)
                .Where(e => e != null)
                .ToArray();
            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }

        public IEnumerator<TResponse> GetEnumerator()
        {
            return Responses
                .Where(r => r != null)
                .SelectMany(cr => cr.Responses)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Responses.GetEnumerator();
        }
    }
}