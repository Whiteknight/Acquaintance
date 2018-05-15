using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public static class OutboxExtensions
    {
        public static IDisposable InitializeOutboxModule(this IMessageBus messageBus, int pollDelayMs = 5000)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            return messageBus.Modules.Add(new OutboxModule(messageBus, pollDelayMs));
        }

        public static IDisposable AddOutboxToBeMonitored(this IMessageBus messageBus, IOutbox outbox)
        {
            var module = GetOutboxModuleOrThrow(messageBus);
            return module.AddOutboxToBeMonitored(outbox);
        }

        private static OutboxModule GetOutboxModuleOrThrow(IMessageBus messageBus)
        {
            var module = messageBus.Modules.Get<OutboxModule>();
            if (module == null)
                throw new Exception($"Outbox module has not been initialized. Call .{nameof(InitializeOutboxModule)} first.");
            return module;
        }

        public static IOutboxFactory GetInMemoryOutboxFactory(this IMessageBus messageBus)
        {
            var module = GetOutboxModuleOrThrow(messageBus);
            return module.GetInMemoryOutboxFactory();
        }

        public static IOutboxFactory GetPassthroughOutboxFactory(this IMessageBus messageBus)
        {
            GetOutboxModuleOrThrow(messageBus);
            return new PassthroughOutboxFactory();
        }

        public static IOutboxManager GetOutboxManager(this IMessageBus messageBus)
        {
            var module = GetOutboxModuleOrThrow(messageBus);
            return module.Manager;
        }
    }
}
