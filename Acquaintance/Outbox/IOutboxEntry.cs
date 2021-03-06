﻿namespace Acquaintance.Outbox
{
    /// <summary>
    /// An entry from an outbox, used when a send is attempted. At the end of the attempt the sender MUST
    /// specify MarkForRetry (if an error prevented the send) or MarkComplete (when the send is successful). 
    /// Failure to call one or the other may lead to blockage or memory leakage or both.
    /// </summary>
    public interface IOutboxEntry
    {
        /// <summary>
        /// The send failed or was not attempted, and the message needs to be retried later
        /// </summary>
        void MarkForRetry();

        /// <summary>
        /// The send completed successfully and the message can be removed from the outbox
        /// </summary>
        void MarkComplete();
    }

    /// <inheritdoc />
    /// <summary>
    /// Includes type information and the envelope to send.
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public interface IOutboxEntry<TPayload> : IOutboxEntry
    {
        /// <summary>
        /// The message to send
        /// </summary>
        Envelope<TPayload> Envelope { get; }
    }

}