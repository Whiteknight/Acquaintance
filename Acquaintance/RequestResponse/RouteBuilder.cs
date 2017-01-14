using Acquaintance.PubSub;
using System;
using System.Collections.Generic;

namespace Acquaintance.RequestResponse
{
    public class RouteBuilder<TRequest, TResponse>
    {
        private readonly IReqResBus _messageBus;
        private readonly List<EventRoute<TRequest>> _routes;
        private string _defaultRoute;

        public RouteBuilder(IReqResBus messageBus)
        {
            _messageBus = messageBus;
            _routes = new List<EventRoute<TRequest>>();
        }

        public RouteBuilder<TRequest, TResponse> When(Func<TRequest, bool> predicate, string channelName)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _routes.Add(new EventRoute<TRequest>(channelName, predicate));
            return this;
        }

        public RouteBuilder<TRequest, TResponse> Else(string defaultRoute)
        {
            if (_defaultRoute != null)
                throw new Exception("A default route is already defined");

            _defaultRoute = defaultRoute ?? string.Empty;
            return this;
        }

        public IListener<TRequest, TResponse> BuildListener()
        {
            return new RequestRouter<TRequest, TResponse>(_messageBus, _routes, _defaultRoute);
        }
    }
}