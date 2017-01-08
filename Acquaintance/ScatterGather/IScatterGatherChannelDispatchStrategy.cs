using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public interface IScatterGatherChannelDispatchStrategy : IDisposable
    {
        IScatterGatherChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name);

        IEnumerable<IScatterGatherChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string name);
    }
}