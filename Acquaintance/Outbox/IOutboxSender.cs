namespace Acquaintance.Outbox
{
    /// <summary>
    /// A sender for an outbox. Reads messages from its associated outbox and attempts to send to the destination.
    /// </summary>
    public interface IOutboxSender
    {
        /// <summary>
        /// Try to send messages to the destination. Returns a list of messages successfully sent and also
        /// messages which returned an error
        /// </summary>
        /// <returns></returns>
        IOutboxSendResult TrySend();
    }
}