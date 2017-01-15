using Acquaintance.Common;

namespace Acquaintance.RequestResponse
{
    public interface IDispatchableRequest<out TResponse> : IDispatchable
    {
        TResponse Response { get; }
    }
}