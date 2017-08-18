using System;

namespace Acquaintance
{
    public interface IChannel : IDisposable
    {
        Guid Id { get; }
        void Unsubscribe(Guid id);
    }
}