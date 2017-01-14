using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public class RouteBuilder<TPayload>
    {
        private readonly IPublishable _messageBus;
        private readonly List<EventRoute<TPayload>> _routes;
        private string _defaultRoute;

        public RouteBuilder(IPublishable messageBus)
        {
            _messageBus = messageBus;
            _routes = new List<EventRoute<TPayload>>();
        }

        public RouteBuilder<TPayload> When(Func<TPayload, bool> predicate, string channelName)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _routes.Add(new EventRoute<TPayload>(channelName, predicate));
            return this;
        }

        public RouteBuilder<TPayload> Else(string defaultRoute)
        {
            if (_defaultRoute != null)
                throw new Exception("A default route is already defined");

            _defaultRoute = defaultRoute ?? string.Empty;
            return this;
        }

        public ISubscription<TPayload> BuildSubscription()
        {
            var subscription = new RoutingSubscription<TPayload>(_messageBus, _routes, _defaultRoute);
            return subscription;
        }
    }
}