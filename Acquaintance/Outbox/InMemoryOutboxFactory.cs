using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class InMemoryOutboxFactory : IOutboxFactory
    {
        private readonly IOutboxManager _manager;
        private readonly int _maxMessages;

        public InMemoryOutboxFactory(IOutboxManager manager, int maxMessages)
        {
            Assert.ArgumentNotNull(manager, nameof(manager));
            Assert.IsInRange(maxMessages, nameof(maxMessages), 1, int.MaxValue);

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