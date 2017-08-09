using Acquaintance.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class RoutingSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly List<EventRoute<TPayload>> _routes;
        private readonly IPubSubBus _messageBus;
        private readonly string _defaultRouteOrNull;
        private readonly RouterModeType _modeType;

        public RoutingSubscription(IPubSubBus messageBus, IEnumerable<EventRoute<TPayload>> routes, string defaultRouteOrNull, RouterModeType modeType)
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

        public void Publish(Envelope<TPayload> message)
        {
            switch (_modeType)
            {
                case RouterModeType.FirstMatchingRoute:
                    PublishFirstOrDefault(message);
                    break;
                case RouterModeType.AllMatchingRoutes:
                    PublishAllMatching(message);
                    break;
            }
        }

        private void PublishFirstOrDefault(Envelope<TPayload> message)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(message.Payload));
            if (route != null)
            {
                message = message.RedirectToChannel(route.ChannelName);
                _messageBus.PublishEnvelope(message);
                return;
            }

            if (_defaultRouteOrNull != null)
            {
                message = message.RedirectToChannel(_defaultRouteOrNull);
                _messageBus.PublishEnvelope(message);
            }
        }

        private void PublishAllMatching(Envelope<TPayload> message)
        {
            bool hasMatch = false;
            foreach (var route in _routes.Where(r => r.Predicate(message.Payload)))
            {
                hasMatch = true;
                var newMessage = message.RedirectToChannel(route.ChannelName);
                _messageBus.PublishEnvelope(newMessage);
            }

            if (!hasMatch && _defaultRouteOrNull != null)
            {
                var newMessage = message.RedirectToChannel(_defaultRouteOrNull);
                _messageBus.PublishEnvelope(newMessage);
            }
        }
    }
}
