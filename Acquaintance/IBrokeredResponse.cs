using System.Collections.Generic;

namespace Acquaintance
{
    public interface IBrokeredResponse<out TResponse> : IEnumerable<TResponse>
    {
        IReadOnlyList<TResponse> Responses { get; }
    }
}