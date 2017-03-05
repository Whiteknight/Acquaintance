using Acquaintance.Common;
using System;
using System.Collections.Generic;

namespace Acquaintance.PubSub
{
    public class RouteBuilder<TPayload>
    {
        private readonly IPubSubBus _messageBus;
        private readonly List<EventRoute<TPayload>> _routes;
        private string _defaultRoute;
        private RouterModeType _mode;

        public RouteBuilder(IPubSubBus messageBus)
        {
            _messageBus = messageBus;
            _routes = new List<EventRoute<TPayload>>();
            _mode = RouterModeType.FirstMatchingRoute;
        }

        public RouteBuilder<TPayload> Mode(RouterModeType mode)
        {
            _mode = mode;
            return this;
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
            var subscription = new RoutingSubscription<TPayload>(_messageBus, _routes, _defaultRoute, _mode);
            return subscription;
        }
    }
}