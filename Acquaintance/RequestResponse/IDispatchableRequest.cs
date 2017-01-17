using Acquaintance.Common;
using System;

namespace Acquaintance.RequestResponse
{
    public interface IDispatchableRequest<out TResponse> : IDispatchable
    {
        TResponse Response { get; }
        Guid ListenerId { get; }
    }
}