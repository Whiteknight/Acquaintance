namespace Acquaintance.PubSub
{
    public interface IPubSubChannel<TPayload> : IPubSubChannel
    {
        void Publish(TPayload payload);
        SubscriptionToken Subscribe(ISubscription<TPayload> subscription);
    }

    public interface IPubSubChannel : IChannel
    {
    }
}