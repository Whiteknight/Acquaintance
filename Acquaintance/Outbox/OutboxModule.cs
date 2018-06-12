using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class OutboxModule : IMessageBusModule, IDisposable
    {
        private readonly IBusBase _messageBus;
        public OutboxMonitor Monitor { get; }

        public OutboxModule(IBusBase messageBus, int pollDelayMs)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.IsInRange(pollDelayMs, nameof(pollDelayMs), 1000, int.MaxValue);

            _messageBus = messageBus;
            Monitor = new OutboxMonitor(messageBus.Logger, messageBus.WorkerPool, pollDelayMs);
        }

        public void Start()
        {
            Monitor.Start();
        }

        public void Stop()
        {
            Monitor.Stop();
        }

        public IDisposable AddOutboxToBeMonitored<TMessage>(IOutbox<TMessage> outbox, Action<Envelope<TMessage>> send)
        {
            Assert.ArgumentNotNull(outbox, nameof(outbox));
            Assert.ArgumentNotNull(send, nameof(send));

            var sender = new OutboxSender<TMessage>(_messageBus.Logger, outbox, send);
            return Monitor.AddOutboxToBeMonitored(sender);
        }

        public IDisposable AddOutboxToBeMonitored(IOutboxSender outbox)
        {
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            return Monitor.AddOutboxToBeMonitored(outbox);
        }

        public void Dispose()
        {
            Monitor.Dispose();
        }
    }
}