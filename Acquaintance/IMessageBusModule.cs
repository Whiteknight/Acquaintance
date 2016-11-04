using System;

namespace Acquaintance
{
    public interface IMessageBusModule : IDisposable
    {
        void Attach(IMessageBus messageBus);
        void Unattach();
        void Start();
        void Stop();
    }
}