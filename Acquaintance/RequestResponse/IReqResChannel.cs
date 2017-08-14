using System;

namespace Acquaintance.RequestResponse
{
    public interface IReqResChannel<TRequest, TResponse> : IReqResChannel
    {
        void Request(Envelope<TRequest> envelope, Request<TResponse> request);
        SubscriptionToken Listen(IListener<TRequest, TResponse> listener);
    }

    public interface IReqResChannel : IChannel
    {
        Guid Id { get; }
    }
}