using System.Collections.Generic;

namespace Acquaintance
{
    public interface IBrokeredResponse<out TResponse>
    {
        IReadOnlyList<TResponse> Responses { get; }
    }
}