using System;

namespace Acquaintance.Outbox
{
    public interface IOutboxFactory
    {
        OutboxAndToken<TMessage> Create<TMessage>(Action<Envelope<TMessage>> outputPort);
    }
}