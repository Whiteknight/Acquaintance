using System;

namespace Acquaintance
{
    public interface IMessageBusModule : IDisposable
    {
        /// <summary>
        /// The module is attached to the message bus but not yet started
        /// </summary>
        /// <param name="messageBus"></param>
        void Attach(IMessageBus messageBus);

        /// <summary>
        /// The module is removed from the message bus
        /// </summary>
        void Unattach();

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