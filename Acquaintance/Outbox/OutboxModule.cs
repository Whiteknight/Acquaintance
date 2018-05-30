using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class OutboxModule : IMessageBusModule, IDisposable
    {
        public OutboxManager Manager { get; }

        public OutboxModule(IBusBase messageBus, int pollDelayMs)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.IsInRange(pollDelayMs, nameof(pollDelayMs), 1000, int.MaxValue);

            Manager = new OutboxManager(messageBus.WorkerPool, pollDelayMs);
        }

        public void Start()
        {
            Manager.Start();
        }

        public void Stop()
        {
            Manager.Stop();
        }

        public IDisposable AddOutboxToBeMonitored(IOutbox outbox)
        {
            return Manager.AddOutboxToBeMonitored(outbox);
        }

        public IOutboxFactory GetInMemoryOutboxFactory(int maxMessages = 100)
        {
            // TODO: Should we cache this?
            return new InMemoryOutboxFactory(Manager, maxMessages);
        }

        public void Dispose()
        {
            Manager.Dispose();
        }
    }
}