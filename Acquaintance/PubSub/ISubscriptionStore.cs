using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public interface ISubscriptionStore
    {
        IDisposable AddSubscription<TPayload>(string[] topics, ISubscription<TPayload> subscription);
        IEnumerable<ISubscription<TPayload>> GetSubscriptions<TPayload>(string[] topics);
        void Remove<TPayload>(ISubscription<TPayload> subscription);
    }
}