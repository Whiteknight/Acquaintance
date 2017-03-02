using System;
using System.Linq;

namespace Acquaintance.Sources
{
    public static class EventSourceExtensions
    {
        public static IDisposable RunEventSource(this IMessageBus messageBus, IEventSource source)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            var module = GetModule(messageBus);
            return module.RunEventSource(source);
        }

        private static EventSourceModule GetModule(IMessageBus messageBus)
        {
            var module = messageBus.Modules.Get<EventSourceModule>().FirstOrDefault();
            if (module != null)
                return module;
            module = new EventSourceModule();
            messageBus.Modules.Add(module);
            return module;
        }
    }
}
