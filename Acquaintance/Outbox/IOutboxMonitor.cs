using System;

namespace Acquaintance.Outbox
{
    /// <summary>
    /// Holds references to outboxes and periodically attempts to flush their contents.
    /// </summary>
    public interface IOutboxMonitor
    {
        /// <summary>
        /// Add an outbox to be monitored by the manager and flushed periodically
        /// </summary>
        /// <param name="outbox"></param>
        /// <returns></returns>
        IDisposable AddOutboxToBeMonitored(IOutboxSender outbox);
    }
}