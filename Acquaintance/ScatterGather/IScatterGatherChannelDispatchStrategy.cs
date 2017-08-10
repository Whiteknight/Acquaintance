using System;
using System.Collections.Generic;
using Acquaintance.Logging;

namespace Acquaintance.ScatterGather
{
    public interface IScatterGatherChannelDispatchStrategy : IDisposable
    {
        IScatterGatherChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string topic, ILogger log);

        IEnumerable<IScatterGatherChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string topic);
    }
}