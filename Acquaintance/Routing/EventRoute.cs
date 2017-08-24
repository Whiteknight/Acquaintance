using System;

namespace Acquaintance.Routing
{
    public class EventRoute<TPayload>
    {
        public EventRoute(string topic, Func<TPayload, bool> predicate)
        {
            Predicate = predicate;
            Topic = topic ?? string.Empty;
        }

        public Func<TPayload, bool> Predicate { get; }
        public string Topic { get; }
    }
}