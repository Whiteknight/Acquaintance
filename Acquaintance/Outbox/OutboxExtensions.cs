using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public static class OutboxExtensions
    {
        /// <summary>
        /// Initialize the OutboxModule, for automatic monitoring of outboxes in the system
        /// This method must be called before any other outbox-related methods
        /// </summary>
        /// <param name="messageBus"></param>
        /// <param name="pollDelayMs"></param>
        /// <returns></returns>
        public static IDisposable InitializeOutboxModule(this IBusBase messageBus, int pollDelayMs = 5000)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            return messageBus.Modules.Add(new OutboxModule(messageBus, pollDelayMs));
        }

        /// <summary>
        /// Add the outbox to be monitored by the OutboxModule
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="outbox"></param>
        /// <param name="send"></param>
        /// <returns></returns>
        public static IDisposable AddOutboxToBeMonitored<TMessage>(this IBusBase messageBus, IOutbox<TMessage> outbox, Action<Envelope<TMessage>> send)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(outbox, nameof(outbox));
            Assert.ArgumentNotNull(send, nameof(send));

            var module = GetOutboxModuleOrThrow(messageBus);
            return module.AddOutboxToBeMonitored(outbox, send);
        }

        /// <summary>
        /// Add the outbox to be monitored by the OutboxModule
        /// </summary>
        /// <param name="messageBus"></param>
        /// <param name="outbox"></param>
        /// <returns></returns>
        public static IDisposable AddOutboxToBeMonitored(this IBusBase messageBus, IOutboxSender outbox)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            var module = GetOutboxModuleOrThrow(messageBus);
            return module.AddOutboxToBeMonitored(outbox);
        }

        /// <summary>
        /// Add the outbox to be monitored by the OutboxModule, if the module has been initialized. 
        /// Otherwise do nothing
        /// </summary>
        /// <param name="messageBus"></param>
        /// <param name="outbox"></param>
        /// <returns></returns>
        public static IDisposable TryAddOutboxToBeMonitored(this IBusBase messageBus, IOutboxSender outbox)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            var module = messageBus.Modules.Get<OutboxModule>();
            return module?.AddOutboxToBeMonitored(outbox) ?? new DoNothingDisposable();
        }

        /// <summary>
        /// Factory method for a new InMemoryOutbox which will automatically be monitored by the OutboxModule
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="messageBus"></param>
        /// <param name="send"></param>
        /// <param name="maxMessages"></param>
        /// <returns></returns>
        public static OutboxAndToken<TMessage> GetMonitoredInMemoryOutbox<TMessage>(this IBusBase messageBus, Action<Envelope<TMessage>> send, int maxMessages = 100)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(send, nameof(send));

            var module = GetOutboxModuleOrThrow(messageBus);
            var outbox = new InMemoryOutbox<TMessage>(maxMessages);
            var token = module.AddOutboxToBeMonitored(outbox, send);
            return new OutboxAndToken<TMessage>(outbox, token);
        }

        /// <summary>
        /// Get a direct reference to the OutboxMonitor, which holds the outboxes and periodically flushes them
        /// </summary>
        /// <param name="messageBus"></param>
        /// <returns></returns>
        public static IOutboxMonitor GetOutboxMonitor(this IBusBase messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            var module = GetOutboxModuleOrThrow(messageBus);
            return module.Monitor;
        }

        private static OutboxModule GetOutboxModuleOrThrow(IBusBase messageBus)
        {
            var module = messageBus.Modules.Get<OutboxModule>();
            return module ?? throw new Exception($"Outbox module has not been initialized. Call .{nameof(InitializeOutboxModule)} first.");
        }
    }
}
