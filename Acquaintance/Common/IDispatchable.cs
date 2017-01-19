using System;

namespace Acquaintance.Common
{
    /// <summary>
    /// A dispatchable can be dispatched to a worker thread to be handled. The caller should wait
    /// for the response to be ready, after which point the response can be read
    /// </summary>
    public interface IDispatchable : IDisposable
    {
        bool WaitForResponse();
        bool Success { get; }
        Exception ErrorInformation { get; }
    }
}