namespace Acquaintance.Outbox
{
    public interface IOutbox
    {
        OutboxFlushResult TryFlush();
        int GetQueuedMessageCount();
    }

    public interface IOutbox<TMessage> : IOutbox
    {
        bool AddMessage(Envelope<TMessage> message);
    }
}

