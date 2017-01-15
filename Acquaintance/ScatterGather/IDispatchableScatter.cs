using Acquaintance.Common;

namespace Acquaintance.ScatterGather
{
    public interface IDispatchableScatter<out TResponse> : IDispatchable
    {
        TResponse[] Responses { get; }
    }
}