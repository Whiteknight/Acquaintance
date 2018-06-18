using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.Routing
{
    public class TopicRouter : IPublishTopicRouter, IRequestTopicRouter, IScatterTopicRouter
    {
        private readonly ConcurrentDictionary<string, IRouteRule> _publishRoutes;
        private readonly ConcurrentDictionary<string, IRouteRule> _requestRoutes;
        private readonly ConcurrentDictionary<string, IRouteRule> _scatterRoutes;

        public TopicRouter()
        {
            _publishRoutes = new ConcurrentDictionary<string, IRouteRule>();
            _requestRoutes = new ConcurrentDictionary<string, IRouteRule>();
            _scatterRoutes = new ConcurrentDictionary<string, IRouteRule>();
        }

        public IDisposable AddPublishRouteRule<TPayload>(string[] topics, IRouteRule<TPayload> rule)
        {
            Assert.ArgumentNotNull(rule, nameof(rule));

            var keys = Topics.Canonicalize(topics)
                .Select(t => GetKey<TPayload>(t ?? string.Empty))
                .ToArray();
            foreach (var key in keys)
            {
                if (!_publishRoutes.TryAdd(key, rule))
                    throw new Exception($"Could not add route for Payload={typeof(TPayload).Name} Topic={key}. It may have already been added");
            }
            return new PublishRouteToken<TPayload>(this, keys);
        }

        public string[] RoutePublish<TPayload>(string[] topics, Envelope<TPayload> envelope)
        {
            topics = topics ?? new[] { string.Empty };
            return topics
                .SelectMany(topic => GetRoutesForPublishTopic(topic, envelope))
                .Distinct()
                .ToArray();
        }

        private IEnumerable<string> GetRoutesForPublishTopic<TPayload>(string topic, Envelope<TPayload> envelope)
        {
            var key = GetKey<TPayload>(topic);
            if (!_publishRoutes.TryGetValue(key, out IRouteRule rule))
                return new[] { topic };
            var typedRule = rule as IRouteRule<TPayload>;
            return typedRule?.GetRoute(topic, envelope) ?? new[] { topic };
        }

        public IDisposable AddRequestRouteRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule)
        {
            Assert.ArgumentNotNull(rule, nameof(rule));
            var key = GetKey<TRequest, TResponse>(topic ?? string.Empty);
            bool ok = _requestRoutes.TryAdd(key, rule);
            return ok ? new RequestRouteToken<TRequest, TResponse>(this, key) : (IDisposable)new DoNothingDisposable();
        }

        public string RouteRequest<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
        {
            topic = topic ?? string.Empty;
            var key = GetKey<TRequest, TResponse>(topic);
            if (!_requestRoutes.TryGetValue(key, out IRouteRule rule))
                return topic;
            var typedRule = rule as IRouteRule<TRequest>;
            return typedRule?.GetRoute(topic, envelope)?.FirstOrDefault() ?? topic;
        }

        public IDisposable AddScatterRouteRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule)
        {
            Assert.ArgumentNotNull(rule, nameof(rule));
            var key = GetKey<TRequest, TResponse>(topic ?? string.Empty);
            bool ok = _scatterRoutes.TryAdd(key, rule);
            return ok ? new ScatterRouteToken<TRequest, TResponse>(this, key) : (IDisposable)new DoNothingDisposable();
        }

        public string RouteScatter<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
        {
            topic = topic ?? string.Empty;
            var key = GetKey<TRequest, TResponse>(topic);
            if (!_scatterRoutes.TryGetValue(key, out IRouteRule rule))
                return topic;
            var typedRule = rule as IRouteRule<TRequest>;
            return typedRule?.GetRoute(topic, envelope)?.FirstOrDefault() ?? topic;
        }

        private class PublishRouteToken<TPayload> : DisposeOnceToken
        {
            private readonly TopicRouter _router;
            private readonly string[] _routeKeys;

            public PublishRouteToken(TopicRouter router, string[] routeKeys)
            {
                _router = router;
                _routeKeys = routeKeys;
            }

            protected override void Dispose(bool disposing)
            {
                foreach (var routeKey in _routeKeys)
                    _router._publishRoutes.TryRemove(routeKey, ObjectManagement.TryDispose);
            }

            public override string ToString()
            {
                return $"Publish Route for Type={typeof(TPayload).Name} Topics={string.Join(",", _routeKeys)}";
            }
        }

        private class RequestRouteToken<TRequest, TResponse> : DisposeOnceToken
        {
            private readonly TopicRouter _router;
            private readonly string _route;

            public RequestRouteToken(TopicRouter router, string route)
            {
                _router = router;
                _route = route;
            }

            protected override void Dispose(bool disposing)
            {
                _router._requestRoutes.TryRemove(_route, ObjectManagement.TryDispose);
            }

            public override string ToString()
            {
                return $"Request Route for Request={typeof(TRequest).Name} Response={typeof(TResponse).Name} Topic={_route}";
            }
        }

        private class ScatterRouteToken<TRequest, TResponse> : DisposeOnceToken
        {
            private readonly TopicRouter _router;
            private readonly string _route;

            public ScatterRouteToken(TopicRouter router, string route)
            {
                _router = router;
                _route = route;
            }

            protected override void Dispose(bool disposing)
            {
                _router._scatterRoutes.TryRemove(_route, ObjectManagement.TryDispose);
            }

            public override string ToString()
            {
                return $"Scatter Route for Request={typeof(TRequest).Name} Response={typeof(TResponse).Name} Topic={_route}";
            }
        }

        private static string GetKey<TPayload>(string topic)
        {
            return $"{typeof(TPayload).FullName}:{topic}";
        }

        private static string GetKey<TRequest, TResponse>(string topic)
        {
            return $"{typeof(TRequest).FullName}:{typeof(TResponse).FullName}:{topic}";
        }
    }
}
