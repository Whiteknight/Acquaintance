using System;
using System.Collections.Generic;
using Acquaintance.RequestResponse;

namespace Acquaintance.Dispatching
{
    public interface IReqResChannelDispatchStrategy : IDisposable
    {
        IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name)
            where TRequest : IRequest<TResponse>;

        IEnumerable<IReqResChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string name)
            where TRequest : IRequest<TResponse>;
    }
}