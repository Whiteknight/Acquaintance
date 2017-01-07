using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class EventRoute<TPayload>
    {
        public EventRoute(string channelName, Func<TPayload, bool> predicate)
        {
            Predicate = predicate;
            ChannelName = channelName;
        }

        public Func<TPayload, bool> Predicate { get; }
        public string ChannelName { get; }
    }
}
