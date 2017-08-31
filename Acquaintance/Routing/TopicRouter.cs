using System;
using System.Collections.Concurrent;
using Acquaintance.Utility;

namespace Acquaintance.Routing
{
    public class TopicRouter : IPublishTopicRouter, IRequestTopicRouter, IScatterTopicRouter
    {
        private readonly ConcurrentDictionary<string, IPublishRouteRule> _publishRoutes;
        private readonly ConcurrentDictionary<string, IRequestRouteRule> _requestRoutes;
        private readonly ConcurrentDictionary<string, IScatterRouteRule> _scatterRoutes;

        public TopicRouter()
        {
            _publishRoutes = new ConcurrentDictionary<string, IPublishRouteRule>();
            _requestRoutes = new ConcurrentDictionary<string, IRequestRouteRule>();
            _scatterRoutes = new ConcurrentDictionary<string, IScatterRouteRule>();
        }

        private static string GetKey<TPayload>(string topic)
        {
            return $"{typeof(TPayload).FullName}:{topic}";
        }

        private static string GetKey<TRequest, TResponse>(string topic)
        {
            return $"{typeof(TRequest).FullName}:{typeof(TResponse).FullName}:{topic}";
        }

        public string[] RoutePublish<TPayload>(string topic, Envelope<TPayload> envelope)
        {
            topic = topic ?? string.Empty;
            var key = GetKey<TPayload>(topic);
            if (!_publishRoutes.TryGetValue(key, out IPublishRouteRule rule))
                return new [] { topic };
            var typedRule = rule as IPublishRouteRule<TPayload>;
            return typedRule?.GetRoute(topic, envelope) ?? new[] { topic };
        }

        public string RouteRequest<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
        {
            topic = topic ?? string.Empty;
            var key = GetKey<TRequest, TResponse>(topic);
            if (!_requestRoutes.TryGetValue(key, out IRequestRouteRule rule))
                return topic;
            var typedRule = rule as IRequestRouteRule<TRequest>;
            return typedRule?.GetRoute(topic, envelope) ?? topic;
        }

        public string RouteScatter<TRequest, TResponse>(string topic, Envelope<TRequest> envelope)
        {
            topic = topic ?? string.Empty;
            var key = GetKey<TRequest, TResponse>(topic);
            if (!_scatterRoutes.TryGetValue(key, out IScatterRouteRule rule))
                return topic;
            var typedRule = rule as IScatterRouteRule<TRequest>;
            return typedRule?.GetRoute(topic, envelope) ?? topic;
        }

        private class NoRouteToken : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private class PublishRouteToken : IDisposable
        {
            private readonly TopicRouter _router;
            private readonly string _route;

            public PublishRouteToken(TopicRouter router, string route)
            {
                _router = router;
                _route = route;
            }

            public void Dispose()
            {
                _router._publishRoutes.TryRemove(_route, out IPublishRouteRule rule);
            }
        }

        public IDisposable AddRule<TPayload>(string topic, IPublishRouteRule<TPayload> rule)
        {
            Assert.ArgumentNotNull(rule, nameof(rule));
            var key = GetKey<TPayload>(topic ?? string.Empty);
            bool ok = _publishRoutes.TryAdd(key, rule);
            return ok ? new PublishRouteToken(this, key) : (IDisposable) new NoRouteToken();
        }

        private class RequestRouteToken : IDisposable
        {
            private readonly TopicRouter _router;
            private readonly string _route;

            public RequestRouteToken(TopicRouter router, string route)
            {
                _router = router;
                _route = route;
            }

            public void Dispose()
            {
                _router._requestRoutes.TryRemove(_route, out IRequestRouteRule rule);
            }
        }

        public IDisposable AddRule<TRequest, TResponse>(string topic, IRequestRouteRule<TRequest> rule)
        {
            Assert.ArgumentNotNull(rule, nameof(rule));
            var key = GetKey<TRequest, TResponse>(topic ?? string.Empty);
            bool ok = _requestRoutes.TryAdd(key, rule);
            return ok ? new RequestRouteToken(this, key) : (IDisposable)new NoRouteToken();
        }

        private class ScatterRouteToken : IDisposable
        {
            private readonly TopicRouter _router;
            private readonly string _route;

            public ScatterRouteToken(TopicRouter router, string route)
            {
                _router = router;
                _route = route;
            }

            public void Dispose()
            {
                _router._scatterRoutes.TryRemove(_route, out IScatterRouteRule rule);
            }
        }

        public IDisposable AddRule<TRequest, TResponse>(string topic, IScatterRouteRule<TRequest> rule)
        {
            Assert.ArgumentNotNull(rule, nameof(rule));
            var key = GetKey<TRequest, TResponse>(topic ?? string.Empty);
            bool ok = _scatterRoutes.TryAdd(key, rule);
            return ok ? new ScatterRouteToken(this, key) : (IDisposable)new NoRouteToken();
        }
    }
}
