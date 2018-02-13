namespace Acquaintance
{
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