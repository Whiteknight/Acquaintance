using System;

namespace Acquaintance.Outbox
{
    public class InMemoryOutboxFactory : IOutboxFactory
    {
        private readonly IOutboxManager _manager;
        private readonly int _maxMessages;

        public InMemoryOutboxFactory(IOutboxManager manager, int maxMessages)
        {
            _manager = manager;
            _maxMessages = maxMessages;
        }

        public OutboxAndToken<TMessage> Create<TMessage>(Action<Envelope<TMessage>> outputPort)
        {
            var outbox = new InMemoryOutbox<TMessage>(outputPort, _maxMessages);
            var token = _manager.AddOutboxToBeMonitored(outbox);
            return new OutboxAndToken<TMessage>(outbox, token);
        }
    }
}