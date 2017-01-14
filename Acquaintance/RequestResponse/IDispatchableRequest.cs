using System;

namespace Acquaintance
{
    public interface IDispatchableRequest<out TResponse> : IDisposable
    {
        TResponse Response { get; }
        bool WaitForResponse();
    }
}