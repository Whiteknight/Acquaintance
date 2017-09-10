using System;
using System.Collections.Concurrent;
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

        public string[] RoutePublish<TPayload>(string topic, Envelope<TPayload> envelope)
        {
            topic = topic ?? string.Empty;
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

        private class PublishRouteToken : Utility.DisposeOnceToken
        {
            private readonly TopicRouter _router;
            private readonly string _route;

            public PublishRouteToken(TopicRouter router, string route)
            {
                _router = router;
                _route = route;
            }

            protected override void Dispose(bool disposing)
            {
                _router._publishRoutes.TryRemove(_route, out IRouteRule rule);
            }
        }

        IDisposable IPublishTopicRouter.AddRule<TPayload>(string topic, IRouteRule<TPayload> rule)
        {
            Assert.ArgumentNotNull(rule, nameof(rule));
            var key = GetKey<TPayload>(topic ?? string.Empty);
            bool ok = _publishRoutes.TryAdd(key, rule);
            return ok ? new PublishRouteToken(this, key) : (IDisposable)new NoRouteToken();
        }

        private class RequestRouteToken : Utility.DisposeOnceToken
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

        private class ScatterRouteToken : Utility.DisposeOnceToken
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
