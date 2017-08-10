using System;
using Acquaintance.Logging;

namespace Acquaintance.RequestResponse
{
    public interface IReqResChannelDispatchStrategy : IDisposable
    {
        IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string topic, ILogger log);

        IReqResChannel<TRequest, TResponse> GetExistingChannel<TRequest, TResponse>(string topic);
    }
}