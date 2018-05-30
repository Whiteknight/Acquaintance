using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class PassthroughOutboxFactory : IOutboxFactory
    {
        public OutboxAndToken<TMessage> Create<TMessage>(Action<Envelope<TMessage>> outputPort)
        {
            Assert.ArgumentNotNull(outputPort, nameof(outputPort));
            var outbox = new PassthroughOutbox<TMessage>(outputPort);
            return new OutboxAndToken<TMessage>(outbox, new DoNothingDisposable());
        }
    }
}