using System;
using System.Collections.Generic;
using Acquaintance.Logging;

namespace Acquaintance.PubSub
{
    public interface IPubSubChannelDispatchStrategy : IDisposable
    {
        IPubSubChannel<TPayload> GetChannelForSubscription<TPayload>(string name, ILogger log);

        IEnumerable<IPubSubChannel<TPayload>> GetExistingChannels<TPayload>(string name);
    }
}