using System;

namespace Acquaintance
{
    public interface IDispatchableRequest<out TResponse> : IDisposable
    {
        TResponse Response { get; }
        bool WaitForResponse();
    }

    public interface IDispatchableScatter<out TResponse> : IDisposable
    {
        TResponse[] Responses { get; }
        bool WaitForResponse();
    }
}