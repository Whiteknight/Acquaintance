using System;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Sources
{
    public static class EventSourceExtensions
    {
        public static IDisposable RunEventSource(this IBusBase messageBus, Action<IEventSourceContext, CancellationToken> action)
        {
            return RunEventSource(messageBus, new DelegateEventSource(action));
        }

        public static IDisposable RunEventSource(this IBusBase messageBus, Action<IEventSourceContext> action)
        {
            return RunEventSource(messageBus, new DelegateEventSource(action));
        }

        public static IDisposable RunEventSource(this IBusBase messageBus, IEventSource source)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            var module = GetModule(messageBus);
            return module.RunEventSource(source);
        }

        public static IDisposable InitializeEventSources(this IPubSubBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            return messageBus.Modules.Add(new EventSourceModule(messageBus, messageBus.Logger));
        }

        private static EventSourceModule GetModule(IBusBase messageBus)
        {
            return messageBus.Modules.Get<EventSourceModule>() ?? throw new Exception($"EventSource module is not enabled. Call .{nameof(InitializeEventSources)}() first");
        }
    }
}
