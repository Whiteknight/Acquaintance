using System;

namespace Acquaintance.ScatterGather
{
    public interface IDispatchableScatter<out TResponse> : IDisposable
    {
        TResponse[] Responses { get; }
        bool WaitForResponse();
    }
}