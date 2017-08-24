using System;
using System.Collections.Generic;
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
            ValidateDoesNotHaveDefaultRule();
            _defaultRoute = defaultRoute ?? string.Empty;
            return this;
        }

        public FilterRouteRule<T> Build()
        {
            return new FilterRouteRule<T>(_routes, _defaultRoute);
        }

        private void ValidateDoesNotHaveDefaultRule()
        {
            if (_defaultRoute != null)
                throw new Exception("A default route is already defined");
        }
    }
}