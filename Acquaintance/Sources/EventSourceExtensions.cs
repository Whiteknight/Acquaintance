using System;
using System.Linq;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Sources
{
    public static class EventSourceExtensions
    {
        public static IDisposable RunEventSource(this IMessageBus messageBus, Action<IEventSourceContext, CancellationToken> action)
        {
            return RunEventSource(messageBus, new DelegateEventSource(action));
        }

        public static IDisposable RunEventSource(this IMessageBus messageBus, Action<IEventSourceContext> action)
        {
            return RunEventSource(messageBus, new DelegateEventSource(action));
        }

        public static IDisposable RunEventSource(this IMessageBus messageBus, IEventSource source)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));

            var module = GetModule(messageBus);
            return module.RunEventSource(source);
        }

        public static IDisposable InitializeEventSources(this IMessageBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            return messageBus.Modules.Add(new EventSourceModule());
        }

        private static EventSourceModule GetModule(IMessageBus messageBus)
        {
            var module = messageBus.Modules.Get<EventSourceModule>().FirstOrDefault();
            if (module == null)
                throw new Exception($"EventSource module is not enabled. Call .{nameof(InitializeEventSources)}() first");
            return module;
        }
    }
}
