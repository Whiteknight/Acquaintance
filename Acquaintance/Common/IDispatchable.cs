using System;

namespace Acquaintance.Common
{
    public interface IDispatchable : IDisposable
    {
        bool WaitForResponse();
        bool Success { get; }
        Exception ErrorInformation { get; }
    }
}