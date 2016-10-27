using System;

namespace Acquaintance.Threading
{
    public interface IMessageHandlerThreadContext : IDisposable
    {
        bool ShouldStop { get; }
        void DispatchAction(IThreadAction action);
        void Stop();
        void WaitForEvent(int? timeoutMs = null);
        IThreadAction GetAction();
    }
}