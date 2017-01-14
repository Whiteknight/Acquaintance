using System;

namespace Acquaintance.RequestResponse
{
    public interface IReqResChannel<TRequest, TResponse> : IReqResChannel
    {
        IDispatchableRequest<TResponse> Request(TRequest request);
        SubscriptionToken Listen(IListener<TRequest, TResponse> listener);
    }

    public interface IReqResChannel : IChannel
    {
        Guid Id { get; }
    }
}