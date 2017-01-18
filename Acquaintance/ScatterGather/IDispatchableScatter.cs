using Acquaintance.Common;
using System;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// A complete scatter request, which can be dispatched to a worker thread
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    public interface IDispatchableScatter<out TResponse> : IDispatchable
    {
        TResponse[] Responses { get; }
        Guid ParticipantId { get; }
    }
}