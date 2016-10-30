using System;
using System.Collections.Generic;

namespace Acquaintance.RequestResponse
{
    public interface IReqResChannel<TRequest, TResponse> : IReqResChannel
    {
        IEnumerable<TResponse> Request(TRequest request);
        SubscriptionToken Listen(Func<TRequest, TResponse> act, Func<TRequest, bool> filter, SubscribeOptions options);
    }

    public interface IReqResChannel : IChannel
    {

    }
}