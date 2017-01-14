using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public interface IGatheredResponse<out TResponse> : IEnumerable<TResponse>
    {
        IReadOnlyList<TResponse> Responses { get; }
    }
}