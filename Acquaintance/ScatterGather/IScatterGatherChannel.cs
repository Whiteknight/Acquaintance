using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public interface IScatterGatherChannel<TRequest, TResponse> : IScatterGatherChanne
    {
        IEnumerable<IDispatchableRequest<TResponse>> Request(TRequest request);
        SubscriptionToken Listen(IParticipant<TRequest, TResponse> listener);
    }

    public interface IScatterGatherChanne : IChannel
    {
        Guid Id { get; }
    }
}