using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.Routing
{
    public class FilterRouteRule<T> : IPublishRouteRule<T>, IRequestRouteRule<T>, IScatterRouteRule<T>
    {
        private readonly EventRoute<T>[] _routes;
        private readonly string _defaultRouteOrNull;

        public FilterRouteRule(IEnumerable<EventRoute<T>> routes, string defaultRouteOrNull)
        {
            Assert.ArgumentNotNull(routes, nameof(routes));
            _routes = routes.ToArray();
            _defaultRouteOrNull = defaultRouteOrNull;
        }

        string[] IPublishRouteRule<T>.GetRoute(string topic, Envelope<T> envelope)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(envelope.Payload));
            if (route != null)
                return new[] { route.Topic };

            if (_defaultRouteOrNull != null)
                return new[] { _defaultRouteOrNull };
            return new string[0];
        }

        string IRequestRouteRule<T>.GetRoute(string topic, Envelope<T> envelope)
        {
            return GetRouteInternal(topic, envelope.Payload);
        }

        string IScatterRouteRule<T>.GetRoute(string topic, Envelope<T> envelope)
        {
            return GetRouteInternal(topic, envelope.Payload);
        }

        private string GetRouteInternal(string topic, T payload)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(payload));
            return route != null ? route.Topic : _defaultRouteOrNull;
        }
    }
}
