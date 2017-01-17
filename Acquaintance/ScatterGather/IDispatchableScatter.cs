using Acquaintance.Common;
using System;

namespace Acquaintance.ScatterGather
{
    public interface IDispatchableScatter<out TResponse> : IDispatchable
    {
        TResponse[] Responses { get; }
        Guid ParticipantId { get; }
    }
}