namespace Acquaintance.Outbox
{
    public interface IOutboxSender
    {
        IOutboxSendResult TrySend();
    }
}