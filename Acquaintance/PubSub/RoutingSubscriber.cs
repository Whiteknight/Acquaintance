using Acquaintance.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class RoutingSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly List<EventRoute<TPayload>> _routes;
        private readonly IPublishable _messageBus;
        private readonly string _defaultRouteOrNull;
        private readonly RouterModeType _modeType;

        public RoutingSubscription(IPublishable messageBus, IEnumerable<EventRoute<TPayload>> routes, string defaultRouteOrNull, RouterModeType modeType)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            if (routes == null)
                throw new ArgumentNullException(nameof(routes));

            // TODO: Try to detect circular references?
            _routes = routes.ToList();
            _messageBus = messageBus;
            _defaultRouteOrNull = defaultRouteOrNull;
            _modeType = modeType;
        }

        public Guid Id { get; set; }
        public bool ShouldUnsubscribe => false;

        public void Publish(TPayload payload)
        {
            switch (_modeType)
            {
                case RouterModeType.FirstMatchingRoute:
                    PublishFirstOrDefault(payload);
                    break;
                case RouterModeType.AllMatchingRoutes:
                    PublishAllMatching(payload);
                    break;
                default:
                    break;
            }
        }

        private void PublishFirstOrDefault(TPayload payload)
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

        private void PublishAllMatching(TPayload payload)
        {
            bool hasMatch = false;
            foreach (var route in _routes.Where(r => r.Predicate(payload)))
            {
                hasMatch = true;
                _messageBus.Publish(route.ChannelName, payload);
            }

            if (!hasMatch && _defaultRouteOrNull != null)
                _messageBus.Publish(_defaultRouteOrNull, payload);
        }
    }
}
