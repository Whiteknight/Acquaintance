using System;

namespace Acquaintance.RequestResponse
{
    public interface IReqResChannelDispatchStrategy : IDisposable
    {
        IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name);

        IReqResChannel<TRequest, TResponse> GetExistingChannel<TRequest, TResponse>(string name);
    }
}