using System.Collections.Generic;

namespace Acquaintance.Outbox
{
    public interface IOutbox
    {
        int GetQueuedMessageCount();
    }

    public interface IOutbox<TPayload> : IOutbox
    {
        bool AddMessage(Envelope<TPayload> message);
        IOutboxEntry<TPayload>[] GetNextQueuedMessages(int max);
    }
}

