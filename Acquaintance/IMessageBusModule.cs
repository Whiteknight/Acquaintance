namespace Acquaintance
{
    /// <summary>
    /// A module for the message bus which may add additional capabilities
    /// </summary>
    public interface IMessageBusModule
    {
        /// <summary>
        /// The module is started
        /// </summary>
        void Start();

        /// <summary>
        /// The module is stopped
        /// </summary>
        void Stop();
    }
}