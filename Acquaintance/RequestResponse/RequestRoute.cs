using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class RequestRoute<TRequest>
    {
        public RequestRoute(string channelName, Func<TRequest, bool> predicate)
        {
            Predicate = predicate;
            ChannelName = channelName;
        }

        public Func<TRequest, bool> Predicate { get; }
        public string ChannelName { get; }
    }
}
