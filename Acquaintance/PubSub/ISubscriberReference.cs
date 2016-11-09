namespace Acquaintance.PubSub
{
    public interface ISubscriberReference<in TPayload>
    {
        void Invoke(TPayload payload);
        bool IsAlive { get; }
    }
}