using System;
using System.Collections.Generic;
using Acquaintance.PubSub;

namespace Acquaintance.Dispatching
{
    public interface IPubSubChannelDispatchStrategy : IDisposable
    {
        IPubSubChannel<TPayload> GetChannelForSubscription<TPayload>(string name);

        IEnumerable<IPubSubChannel<TPayload>> GetExistingChannels<TPayload>(string name, TPayload payload);
    }
}