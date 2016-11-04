using System;

namespace Acquaintance.Threading
{
    public interface IActionDispatcher
    {
        void DispatchAction(IThreadAction action);
    }

    public interface IMessageHandlerThreadContext : IActionDispatcher, IDisposable
    {
        bool ShouldStop { get; }
        void Stop();
        IThreadAction GetAction(int? timeoutMs = null);
    }
}