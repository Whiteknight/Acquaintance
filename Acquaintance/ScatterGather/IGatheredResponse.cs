using Acquaintance.Common;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// The gathered responses from a scatter request
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    public interface IGatheredResponse<TResponse> : IEnumerable<TResponse>
    {
        IReadOnlyList<CompleteGather<TResponse>> Responses { get; }

        void ThrowAnyExceptions();
    }
}