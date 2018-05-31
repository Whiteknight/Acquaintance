namespace Acquaintance.Outbox
{
    public interface IOutboxEntry
    {
        void MarkForRetry();
        void MarkComplete();
    }

    public interface IOutboxEntry<TPayload> : IOutboxEntry
    {
        Envelope<TPayload> Envelope { get; }
    }

}