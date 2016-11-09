namespace Acquaintance.PubSub
{
    public interface ISubscription<in TPayload>
    {
        void Publish(TPayload payload);
        bool ShouldUnsubscribe { get; }
    }
}