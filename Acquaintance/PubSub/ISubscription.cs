using System;

namespace Acquaintance.PubSub
{
    /// <summary>
    /// A subscription for a pub/sub channel. The subscription receives all
    /// messages published on its channel.
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public interface ISubscription<TPayload> : IDisposable
    {
        /// <summary>
        /// Receive the published message and perform the necessary action
        /// </summary>
        /// <param name="message"></param>
        void Publish(Envelope<TPayload> message);

        /// <summary>
        /// True if this subscription should be removed from the channel, such as
        /// hitting a message limit or an error condition. False otherwise.
        /// </summary>
        bool ShouldUnsubscribe { get; }

        /// <summary>
        /// The unique ID of this subscription. This value is set and maintained 
        /// by the channel
        /// </summary>
        Guid Id { get; set; }
    }
}