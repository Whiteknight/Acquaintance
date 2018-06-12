namespace Acquaintance.Outbox
{
    public enum OutboxSendResultType
    {
        /// <summary>
        /// The send was successful
        /// </summary>
        SendSuccess,

        /// <summary>
        /// There were no messages to send
        /// </summary>
        NoMessages,

        /// <summary>
        /// No attempt was made to send the message, possibly because of a previous error
        /// </summary>
        NotAttempted,

        /// <summary>
        /// The send failed and may have provided error information
        /// </summary>
        SendFailed
    }
}