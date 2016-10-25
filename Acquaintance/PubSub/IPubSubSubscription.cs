namespace Acquaintance.PubSub
{
    public interface IPubSubSubscription<in TPayload>
    {
        void Publish(TPayload payload);
    }
}