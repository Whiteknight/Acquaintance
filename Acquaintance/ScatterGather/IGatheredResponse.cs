using Acquaintance.Common;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public interface IGatheredResponse<TResponse> : IEnumerable<TResponse>
    {
        IReadOnlyList<CompleteGather<TResponse>> Responses { get; }

        void ThrowAnyExceptions();
    }
}