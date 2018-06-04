using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class OutboxModule : IMessageBusModule, IDisposable
    {
        private readonly IBusBase _messageBus;
        public OutboxManager Manager { get; }

        public OutboxModule(IBusBase messageBus, int pollDelayMs)
        {
            _messageBus = messageBus;
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.IsInRange(pollDelayMs, nameof(pollDelayMs), 1000, int.MaxValue);

            Manager = new OutboxManager(messageBus.Logger, messageBus.WorkerPool, pollDelayMs);
        }

        public void Start()
        {
            Manager.Start();
        }

        public void Stop()
        {
            Manager.Stop();
        }

        public IDisposable AddOutboxToBeMonitored<TMessage>(IOutbox<TMessage> outbox, Action<Envelope<TMessage>> send)
        {
            Assert.ArgumentNotNull(outbox, nameof(outbox));
            Assert.ArgumentNotNull(send, nameof(send));

            var sender = new OutboxSender<TMessage>(_messageBus.Logger, outbox, send);
            return Manager.AddOutboxToBeMonitored(sender);
        }

        public IDisposable AddOutboxToBeMonitored(IOutboxSender outbox)
        {
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            return Manager.AddOutboxToBeMonitored(outbox);
        }

        public void Dispose()
        {
            Manager.Dispose();
        }
    }
}