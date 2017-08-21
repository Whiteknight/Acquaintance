using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public interface ISubscriptionStore
    {
        IDisposable AddSubscription<TPayload>(string topic, ISubscription<TPayload> subscription);
        IEnumerable<ISubscription<TPayload>> GetSubscriptions<TPayload>(string topic);
    }
}