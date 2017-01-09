using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public class RouteBuilder<TPayload>
    {
        private readonly List<EventRoute<TPayload>> _routes;

        public RouteBuilder(List<EventRoute<TPayload>> routes)
        {
            _routes = routes;
        }

        public RouteBuilder<TPayload> When(Func<TPayload, bool> predicate, string channelName)
        {
            _routes.Add(new EventRoute<TPayload>(channelName, predicate));
            return this;
        }
    }
}