using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public static class OutboxExtensions
    {
        public static IDisposable InitializeOutboxModule(this IBusBase messageBus, int pollDelayMs = 5000)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            return messageBus.Modules.Add(new OutboxModule(messageBus, pollDelayMs));
        }

        public static IDisposable AddOutboxToBeMonitored<TMessage>(this IBusBase messageBus, IOutbox<TMessage> outbox, Action<Envelope<TMessage>> send)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(outbox, nameof(outbox));
            Assert.ArgumentNotNull(send, nameof(send));

            var module = GetOutboxModuleOrThrow(messageBus);
            return module.AddOutboxToBeMonitored(outbox, send);
        }

        public static IDisposable AddOutboxToBeMonitored(this IBusBase messageBus, IOutboxSender outbox)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            var module = GetOutboxModuleOrThrow(messageBus);
            return module.AddOutboxToBeMonitored(outbox);
        }

        public static IDisposable TryAddOutboxToBeMonitored(this IBusBase messageBus, IOutboxSender outbox)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            var module = messageBus.Modules.Get<OutboxModule>();
            if (module == null)
                return new DoNothingDisposable();
            return module.AddOutboxToBeMonitored(outbox);
        }

        public static OutboxAndToken<TMessage> GetMonitoredInMemoryOutbox<TMessage>(this IBusBase messageBus, Action<Envelope<TMessage>> send, int maxMessages = 100)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(send, nameof(send));

            var module = GetOutboxModuleOrThrow(messageBus);
            var outbox = new InMemoryOutbox<TMessage>(maxMessages);
            var token = module.AddOutboxToBeMonitored(outbox, send);
            return new OutboxAndToken<TMessage>(outbox, token);
        }

        public static IOutboxManager GetOutboxManager(this IBusBase messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            var module = GetOutboxModuleOrThrow(messageBus);
            return module.Manager;
        }

        private static OutboxModule GetOutboxModuleOrThrow(IBusBase messageBus)
        {
            var module = messageBus.Modules.Get<OutboxModule>();
            return module ?? throw new Exception($"Outbox module has not been initialized. Call .{nameof(InitializeOutboxModule)} first.");
        }
    }
}
