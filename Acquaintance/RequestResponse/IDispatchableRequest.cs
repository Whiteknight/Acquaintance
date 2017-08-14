using Acquaintance.Common;
using System;

namespace Acquaintance.RequestResponse
{
    /// <summary>
    /// A dispatchable request. Wait for the request to be completed and then read the response and
    /// status information
    /// </summary>
    public interface IDispatchableRequest
    {
        Guid ListenerId { get; }
    }
}