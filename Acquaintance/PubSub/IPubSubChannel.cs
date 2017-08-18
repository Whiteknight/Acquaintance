namespace Acquaintance.PubSub
{
    public interface IPubSubChannel<TPayload> : IPubSubChannel
    {
        void Publish(Envelope<TPayload> message);
        SubscriptionToken Subscribe(ISubscription<TPayload> subscription);
    }

    public interface IPubSubChannel : IChannel
    {
    }
}