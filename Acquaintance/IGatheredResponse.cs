using System.Collections.Generic;

namespace Acquaintance
{
    public interface IGatheredResponse<out TResponse> : IEnumerable<TResponse>
    {
        IReadOnlyList<TResponse> Responses { get; }
    }
}