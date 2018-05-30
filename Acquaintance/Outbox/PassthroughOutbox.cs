using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class PassthroughOutbox<TMessage> : IOutbox<TMessage>
    {
        private readonly Action<Envelope<TMessage>> _outputPort;

        public PassthroughOutbox(Action<Envelope<TMessage>> outputPort)
        {
            Assert.ArgumentNotNull(outputPort, nameof(outputPort));
            _outputPort = outputPort;
        }

        public bool AddMessage(Envelope<TMessage> message)
        {
            Assert.ArgumentNotNull(message, nameof(message));
            _outputPort(message);
            return true;
        }

        public OutboxFlushResult TryFlush()
        {
            return OutboxFlushResult.Success();
        }

        public int GetQueuedMessageCount()
        {
            return 0;
        }
    }
}