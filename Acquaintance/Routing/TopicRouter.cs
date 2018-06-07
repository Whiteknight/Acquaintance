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

        private static string GetKey<TPayload>(string topic)
        {
            return $"{typeof(TPayload).FullName}:{topic}";
        }

        private static string GetKey<TRequest, TResponse>(string topic)
        {
            return $"{typeof(TRequest).FullName}:{typeof(TResponse).FullName}:{topic}";
        }

        public string[] RoutePublish<TPayload>(string[] topics, Envelope<TPayload> envelope)
        {
            topics = topics ?? new[] { string.Empty };
            return topics.SelectMany(topic => GetRoutesForPublishTopic(topic, envelope)).Distinct().ToArray();
        }

        private IEnumerable<string> GetRoutesForPublishTopic<TPayload>(string topic, Envelope<TPayload> envelope)
        {
            var key = GetKey<TPayload>(topic);
            if (!_publishRoutes.TryGetValue(key, out IRouteRule rule))
                return new[] { topic };
            var typedRule = rule as IRouteRule<TPayload>;
            return typedRule?.GetRoute(topic, envelope) ?? new[] { topic };
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

        public string RouteScatter<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
        {
            topic = topic ?? string.Empty;
            var key = GetKey<TRequest, TResponse>(topic);
            if (!_scatterRoutes.TryGetValue(key, out IRouteRule rule))
                return topic;
            var typedRule = rule as IRouteRule<TRequest>;
            return typedRule?.GetRoute(topic, envelope)?.FirstOrDefault() ?? topic;
        }

        private class NoRouteToken : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private class PublishRouteToken : DisposeOnceToken
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
                    _router._publishRoutes.TryRemove(routeKey, out IRouteRule rule);
            }
        }

        IDisposable IPublishTopicRouter.AddRule<TPayload>(string[] topics, IRouteRule<TPayload> rule)
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
            return new PublishRouteToken(this, keys);
        }

        private class RequestRouteToken : DisposeOnceToken
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
                _router._requestRoutes.TryRemove(_route, out IRouteRule rule);
            }
        }

        IDisposable IRequestTopicRouter.AddRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule)
        {
            Assert.ArgumentNotNull(rule, nameof(rule));
            var key = GetKey<TRequest, TResponse>(topic ?? string.Empty);
            bool ok = _requestRoutes.TryAdd(key, rule);
            return ok ? new RequestRouteToken(this, key) : (IDisposable)new NoRouteToken();
        }

        private class ScatterRouteToken : DisposeOnceToken
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
                _router._scatterRoutes.TryRemove(_route, out IRouteRule rule);
            }
        }

        IDisposable IScatterTopicRouter.AddRule<TRequest, TResponse>(string topic, IRouteRule<TRequest> rule)
        {
            Assert.ArgumentNotNull(rule, nameof(rule));
            var key = GetKey<TRequest, TResponse>(topic ?? string.Empty);
            bool ok = _scatterRoutes.TryAdd(key, rule);
            return ok ? new ScatterRouteToken(this, key) : (IDisposable)new NoRouteToken();
        }
    }
}
