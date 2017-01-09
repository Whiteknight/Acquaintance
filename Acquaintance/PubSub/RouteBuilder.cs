using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public class RouteBuilder<TPayload>
    {
        // TODO: A default option, in case all predicates fail. .Else()
        // TOOD: A mode toggle, whether to publish to all matching routes, or only to the first one.
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