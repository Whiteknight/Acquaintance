using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    /// <summary>
    /// Holds subscriptions and enables them to be gotten by payload type and topics
    /// </summary>
    public interface ISubscriptionStore
    {
        /// <summary>
        /// Add a new subscription to the store, to listen on the given payload type and topics
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="topics"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        IDisposable AddSubscription<TPayload>(string[] topics, ISubscription<TPayload> subscription);

        /// <summary>
        /// Get subscriptions which are registered for the given payload type and topics
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="topics"></param>
        /// <returns></returns>
        IEnumerable<ISubscription<TPayload>> GetSubscriptions<TPayload>(string[] topics);

        /// <summary>
        /// Remove the subscription from the store
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="subscription"></param>
        void Remove<TPayload>(ISubscription<TPayload> subscription);
    }
}