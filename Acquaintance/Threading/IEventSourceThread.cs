using System;

namespace Acquaintance.Threading
{
    public interface IEventSourceThread : IDisposable
    {
        Guid Id { get; }
        int ThreadId { get; }
    }
}