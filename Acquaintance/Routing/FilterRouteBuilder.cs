using System;
using System.Collections.Generic;
using Acquaintance.Utility;

namespace Acquaintance.Routing
{
    public class FilterRouteBuilder<T> : IFilterRouteBuilderSingleInput<T>, IFilterRouteBuilderMultiInput<T>, IFilterRouteBuilderWhen<T>
    {
        private readonly List<EventRoute<T>> _routes;
        private string _defaultRoute;

        public string[] InTopics { get; private set; }

        public FilterRouteBuilder()
        {
            _routes = new List<EventRoute<T>>();
        }

        public FilterRouteRule<T> Build()
        {
            return new FilterRouteRule<T>(_routes, _defaultRoute);
        }

        public IFilterRouteBuilderWhen<T> FromTopics(params string[] topics)
        {
            InTopics = Topics.Canonicalize(topics);
            return this;
        }

        public IFilterRouteBuilderWhen<T> FromTopics(IEnumerable<string> topics)
        {
            InTopics = Topics.Canonicalize(topics);
            return this;
        }

        public IFilterRouteBuilderWhen<T> FromTopic(string topic)
        {
            throw new NotImplementedException();
        }

        public IFilterRouteBuilderWhen<T> FromDefaultTopic()
        {
            InTopics = new[] { string.Empty };
            return this;
        }

        public IFilterRouteBuilderWhen<T> When(Func<T, bool> predicate, string topic)
        {
            Assert.ArgumentNotNull(predicate, nameof(predicate));

            _routes.Add(new EventRoute<T>(topic, predicate));
            return this;
        }

        public void Else(string defaultRoute)
        {
            ValidateDoesNotHaveDefaultRule();
            _defaultRoute = defaultRoute ?? string.Empty;
        }

        private void ValidateDoesNotHaveDefaultRule()
        {
            if (_defaultRoute != null)
                throw new Exception("A default route is already defined");
        }
    }
}