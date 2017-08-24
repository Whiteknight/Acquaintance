using System;
using Acquaintance.Logging;

namespace Acquaintance.Threading
{
    public interface IWorkerContext : IActionDispatcher, IDisposable
    {
        ILogger Log { get; }
        bool ShouldStop { get; }
        void Stop();
        IThreadAction GetAction(int? timeoutMs = null);
    }
}