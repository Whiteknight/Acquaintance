using Acquaintance.PubSub;
using System;
using System.Collections.Generic;

namespace Acquaintance.Dispatching
{
    public interface IPubSubChannelDispatchStrategy : IDisposable
    {
        IPubSubChannel<TPayload> GetChannelForSubscription<TPayload>(string name);

        IEnumerable<IPubSubChannel<TPayload>> GetExistingChannels<TPayload>(string name);
    }
}