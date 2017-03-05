namespace Acquaintance.PubSub
{
    public interface ISubscriberReference<TPayload>
    {
        void Invoke(Envelope<TPayload> message);
        bool IsAlive { get; }
    }
}