using System;

namespace Acquaintance.Outbox
{
    public interface IOutboxManager
    {
        IDisposable AddOutboxToBeMonitored(IOutboxSender outbox);
    }
}