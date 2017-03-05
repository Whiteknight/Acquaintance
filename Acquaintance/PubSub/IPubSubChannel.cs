using System;

namespace Acquaintance.PubSub
{
    public interface IPubSubChannel<TPayload> : IPubSubChannel
    {
        Guid Id { get; }
        void Publish(Envelope<TPayload> message);
        SubscriptionToken Subscribe(ISubscription<TPayload> subscription);
    }

    public interface IPubSubChannel : IChannel
    {
    }
}