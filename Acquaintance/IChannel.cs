using System;

namespace Acquaintance
{
    public interface IChannel : IDisposable
    {
        void Unsubscribe(Guid id);
    }
}