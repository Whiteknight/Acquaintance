using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class RoutingSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly List<EventRoute<TPayload>> _routes;
        private readonly IPublishable _messageBus;
        private readonly string _defaultRouteOrNull;

        public RoutingSubscription(IPublishable messageBus, IEnumerable<EventRoute<TPayload>> routes, string defaultRouteOrNull)
        {
            if (messageBus == null)
                throw new System.ArgumentNullException(nameof(messageBus));

            if (routes == null)
                throw new System.ArgumentNullException(nameof(routes));

            // TODO: Try to detect circular references?
            _routes = routes.ToList();
            _messageBus = messageBus;
            _defaultRouteOrNull = defaultRouteOrNull;
        }

        public bool ShouldUnsubscribe => false;

        public void Publish(TPayload payload)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(payload));
            if (route != null)
            {
                _messageBus.Publish(route.ChannelName, payload);
                return;
            }

            if (_defaultRouteOrNull != null)
                _messageBus.Publish(_defaultRouteOrNull, payload);

        }
    }
}
