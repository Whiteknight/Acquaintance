using System;

namespace Acquaintance.PubSub
{
    public interface IPubSubChannel<TPayload> : IPubSubChannel
    {
        void Publish(TPayload payload);
        SubscriptionToken Subscribe(Action<TPayload> act, Func<TPayload, bool> filter, SubscribeOptions options);
    }

    public interface IPubSubChannel : IChannel
    {
    }
}