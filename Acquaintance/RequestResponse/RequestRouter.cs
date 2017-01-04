using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class RequestRouter<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly string _sourceName;
        private readonly List<RequestRoute> _routes;
        private readonly IReqResBus _messageBus;

        public RequestRouter(IReqResBus messageBus, string sourceName)
        {
            _sourceName = sourceName ?? string.Empty;
            _routes = new List<RequestRoute>();
            _messageBus = messageBus;
        }

        public IDisposable Token { get; set; }

        internal void SetToken(IDisposable token)
        {
            Token = new Subscription(this, token);
        }

        private class RequestRoute
        {
            public RequestRoute(string channelName, Func<TRequest, bool> predicate)
            {
                Predicate = predicate;
                ChannelName = channelName;
            }

            public Func<TRequest, bool> Predicate { get; }
            public string ChannelName { get; }
        }

        private class Subscription : IDisposable
        {
            private readonly RequestRouter<TRequest, TResponse> _router;
            private readonly IDisposable _token;

            public Subscription(RequestRouter<TRequest, TResponse> router, IDisposable token)
            {
                _router = router;
                _token = token;
            }

            public void Dispose()
            {
                _token.Dispose();
            }
        }

        public RequestRouter<TRequest, TResponse> Route(string name, Func<TRequest, bool> predicate, ListenOptions options = null)
        {
            name = name ?? string.Empty;
            if (name == _sourceName)
                throw new Exception("Circular reference detected. Cannot route on the same channel name.");

            _routes.Add(new RequestRoute(name, predicate));
            return this;
        }

        public bool CanHandle(TRequest request)
        {
            // TODO: Add Filtering
            return true;
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            RequestRoute route = _routes.FirstOrDefault(r => r.Predicate(request));
            if (route == null)
                return new ImmediateResponse<TResponse>(null);
            var response = _messageBus.Request<TRequest, TResponse>(route.ChannelName, request);
            return new ImmediateResponse<TResponse>(new[] { response });
        }

        public bool ShouldStopListening => false;
    }

    public class ScatterRouter<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly string _sourceName;
        private readonly List<RequestRoute> _routes;
        private readonly IReqResBus _messageBus;

        public ScatterRouter(IReqResBus messageBus, string sourceName)
        {
            _sourceName = sourceName ?? string.Empty;
            _routes = new List<RequestRoute>();
            _messageBus = messageBus;
        }

        public IDisposable Token { get; set; }

        internal void SetToken(IDisposable token)
        {
            Token = new Subscription(this, token);
        }

        private class RequestRoute
        {
            public RequestRoute(string channelName, Func<TRequest, bool> predicate)
            {
                Predicate = predicate;
                ChannelName = channelName;
            }

            public Func<TRequest, bool> Predicate { get; }
            public string ChannelName { get; }
        }

        private class Subscription : IDisposable
        {
            private readonly ScatterRouter<TRequest, TResponse> _router;
            private readonly IDisposable _token;

            public Subscription(ScatterRouter<TRequest, TResponse> router, IDisposable token)
            {
                _router = router;
                _token = token;
            }

            public void Dispose()
            {
                _token.Dispose();
            }
        }

        public ScatterRouter<TRequest, TResponse> Route(string name, Func<TRequest, bool> predicate, ListenOptions options = null)
        {
            name = name ?? string.Empty;
            if (name == _sourceName)
                throw new Exception("Circular reference detected. Cannot route on the same channel name.");

            _routes.Add(new RequestRoute(name, predicate));
            return this;
        }

        public bool CanHandle(TRequest request)
        {
            // TODO: Add Filtering
            return true;
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            var routes = _routes.Where(r => r.Predicate(request));
            List<TResponse> responses = new List<TResponse>();
            foreach (var route in routes)
            {
                var response = _messageBus.Scatter<TRequest, TResponse>(route.ChannelName, request);
                responses.AddRange(response.ToArray());
            }
            return new ImmediateResponse<TResponse>(responses.ToArray());
        }

        public bool ShouldStopListening => false;
    }
}
