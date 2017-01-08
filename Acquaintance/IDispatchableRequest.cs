using System;

namespace Acquaintance
{
    public interface IDispatchableRequest<out TResponse> : IDisposable
    {
        TResponse[] Responses { get; }
        bool WaitForResponse();
    }
}