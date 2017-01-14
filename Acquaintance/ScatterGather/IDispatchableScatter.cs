using System;

namespace Acquaintance
{

    public interface IDispatchableScatter<out TResponse> : IDisposable
    {
        TResponse[] Responses { get; }
        bool WaitForResponse();
    }
}