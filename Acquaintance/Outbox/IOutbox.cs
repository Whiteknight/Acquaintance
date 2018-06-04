namespace Acquaintance.Outbox
{
    /// <summary>
    /// Cache for local storage of messages which may need to be retried until a send is successful
    /// </summary>
    public interface IOutbox
    {
        int GetQueuedMessageCount();
    }

    /// <inheritdoc />
    /// <summary>
    /// Typed for a single message payload type.
    /// </summary>
    public interface IOutbox<TPayload> : IOutbox
    {
        /// <summary>
        /// Add a new message to the outbox, to be sent at the next possible time
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool AddMessage(Envelope<TPayload> message);

        /// <summary>
        /// Get entries from the outbox to attempt send. This operation may be strictly limited to operate on a single thread at a time.
        /// </summary>
        /// <param name="max"></param>
        /// <returns></returns>
        IOutboxEntry<TPayload>[] GetNextQueuedMessages(int max);
    }
}

