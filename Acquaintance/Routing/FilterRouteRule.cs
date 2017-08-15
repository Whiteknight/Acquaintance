using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.Routing
{
    public class FilterRouteBuilder<T>
    {
        private readonly List<EventRoute<T>> _routes;
        private string _defaultRoute;

        public FilterRouteBuilder()
        {
            _routes = new List<EventRoute<T>>();
        }

        public FilterRouteBuilder<T> When(Func<T, bool> predicate, string topic)
        {
            Assert.ArgumentNotNull(predicate, nameof(predicate));

            _routes.Add(new EventRoute<T>(topic, predicate));
            return this;
        }

        public FilterRouteBuilder<T> Else(string defaultRoute)
        {
            if (_defaultRoute != null)
                throw new Exception("A default route is already defined");

            _defaultRoute = defaultRoute ?? string.Empty;
            return this;
        }

        public FilterRouteRule<T> Build()
        {
            return new FilterRouteRule<T>(_routes, _defaultRoute);
        }
    }

    public class EventRoute<TPayload>
    {
        public EventRoute(string topic, Func<TPayload, bool> predicate)
        {
            Predicate = predicate;
            Topic = topic ?? string.Empty;
        }

        public Func<TPayload, bool> Predicate { get; }
        public string Topic { get; }
    }

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
            if (route != null)
                return route.Topic;

            return _defaultRouteOrNull;
        }
    }
}
