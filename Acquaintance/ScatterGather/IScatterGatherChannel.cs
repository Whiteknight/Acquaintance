using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public interface IScatterGatherChannel<TRequest, TResponse> : IScatterGatherChannel
    {
        IEnumerable<IDispatchableScatter<TResponse>> Scatter(TRequest request);
        SubscriptionToken Participate(IParticipant<TRequest, TResponse> participant);
    }

    public interface IScatterGatherChannel : IChannel
    {
        Guid Id { get; }
    }
}