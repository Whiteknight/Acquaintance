using System;

namespace Acquaintance.Threading
{
    public interface IEventSourceWorker : IDisposable
    {
        Guid Id { get; }
        int ThreadId { get; }
    }
}