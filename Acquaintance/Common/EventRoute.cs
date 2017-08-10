using System;

namespace Acquaintance.Common
{
    public class EventRoute<TPayload>
    {
        public EventRoute(string topic, Func<TPayload, bool> predicate)
        {
            Predicate = predicate;
            Topic = topic;
        }

        public Func<TPayload, bool> Predicate { get; }
        public string Topic { get; }
    }
}
