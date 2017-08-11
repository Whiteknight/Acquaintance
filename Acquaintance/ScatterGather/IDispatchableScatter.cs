using System;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// A complete scatter request, which can be dispatched to a worker thread
    /// </summary>
    public interface IDispatchableScatter
    {
        Guid ParticipantId { get; }
    }
}