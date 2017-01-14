using System;

namespace Acquaintance.RequestResponse
{
    public interface IDispatchableRequest<out TResponse> : IDisposable
    {
        TResponse Response { get; }
        bool WaitForResponse();
    }
}