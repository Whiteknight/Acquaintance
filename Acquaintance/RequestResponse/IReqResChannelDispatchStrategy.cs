using System;
using Acquaintance.Logging;

namespace Acquaintance.RequestResponse
{
    public interface IReqResChannelDispatchStrategy : IDisposable
    {
        IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name, ILogger log);

        IReqResChannel<TRequest, TResponse> GetExistingChannel<TRequest, TResponse>(string name);
    }
}