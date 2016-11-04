using System;
using System.Collections.Generic;

namespace Acquaintance.RequestResponse
{
    public interface IReqResChannel<TRequest, TResponse> : IReqResChannel
    {
        IEnumerable<IDispatchableRequest<TResponse>> Request(TRequest request);
        SubscriptionToken Listen(IListener<TRequest, TResponse> listener);
    }

    public interface IReqResChannel : IChannel
    {
        Guid Id { get; }
    }
}