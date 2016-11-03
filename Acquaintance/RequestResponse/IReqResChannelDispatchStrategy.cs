using System;
using System.Collections.Generic;

namespace Acquaintance.RequestResponse
{
    public interface IReqResChannelDispatchStrategy : IDisposable
    {
        IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name);

        IEnumerable<IReqResChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string name);
    }
}