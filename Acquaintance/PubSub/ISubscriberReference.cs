namespace Acquaintance.PubSub
{
    /// <summary>
    /// A reference to a subscriber action.
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public interface ISubscriberReference<TPayload>
    {
        /// <summary>
        /// Invoke the action on the given message
        /// </summary>
        /// <param name="message"></param>
        void Invoke(Envelope<TPayload> message);

        /// <summary>
        /// Returns true if the reference is alive and able to be invoked. false otherwise.
        /// </summary>
        bool IsAlive { get; }
    }
}