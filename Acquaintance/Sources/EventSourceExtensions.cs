using System;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Sources
{
    public static class EventSourceExtensions
    {
        /// <summary>
        /// Schedule the given action to run as an event source. The system will invoke it periodically.
        /// </summary>
        /// <param name="messageBus"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IDisposable RunEventSource(this IBusBase messageBus, Action<IEventSourceContext, CancellationToken> action)
        {
            return RunEventSource(messageBus, new DelegateEventSource(action));
        }

        /// <summary>
        /// Schedule the given action to run as an event source. The system will invoke it periodically.
        /// </summary>
        /// <param name="messageBus"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IDisposable RunEventSource(this IBusBase messageBus, Action<IEventSourceContext> action)
        {
            return RunEventSource(messageBus, new DelegateEventSource(action));
        }

        /// <summary>
        /// Schedule the provided event source with the system. It will be polled periodically for new events.
        /// </summary>
        /// <param name="messageBus"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IDisposable RunEventSource(this IBusBase messageBus, IEventSource source)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            var module = GetModuleOrThrow(messageBus);
            return module.RunEventSource(source);
        }

        /// <summary>
        /// Initialize the EventSourceModule. This method must be called before calling any RunEventSource method variant
        /// </summary>
        /// <param name="messageBus"></param>
        /// <returns></returns>
        public static IDisposable InitializeEventSources(this IPubSubBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            return messageBus.Modules.Add(new EventSourceModule(messageBus, messageBus.Logger));
        }

        private static EventSourceModule GetModuleOrThrow(IBusBase messageBus)
        {
            return messageBus.Modules.Get<EventSourceModule>() ?? throw new Exception($"EventSource module is not enabled. Call .{nameof(InitializeEventSources)}() first");
        }
    }
}
