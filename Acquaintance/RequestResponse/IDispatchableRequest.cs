using System;

namespace Acquaintance.RequestResponse
{
    public interface IDispatchableRequest<out TResponse> : IDisposable
    {
        TResponse[] Responses { get; }
        bool WaitForResponse();
    }
}