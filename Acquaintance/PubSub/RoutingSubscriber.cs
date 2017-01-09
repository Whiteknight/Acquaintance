using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class RoutingSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly List<EventRoute<TPayload>> _routes;
        private readonly IPublishable _messageBus;

        public RoutingSubscription(IPublishable messageBus, IEnumerable<EventRoute<TPayload>> routes)
        {
            // TODO: Try to detect circular references?
            _routes = routes.ToList();
            _messageBus = messageBus;
        }

        public bool ShouldUnsubscribe => false;

        public void Publish(TPayload payload)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(payload));
            if (route == null)
                return;

            _messageBus.Publish<TPayload>(route.ChannelName, payload);
        }
    }
}
