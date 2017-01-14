using Acquaintance.Common;
using System;
using System.Collections.Generic;

namespace Acquaintance.ScatterGather
{
    public class RouteBuilder<TRequest, TResponse>
    {
        private readonly IReqResBus _messageBus;
        private readonly List<EventRoute<TRequest>> _routes;
        private string _defaultRoute;
        private RouterModeType _mode;

        public RouteBuilder(IReqResBus messageBus)
        {
            _messageBus = messageBus;
            _routes = new List<EventRoute<TRequest>>();
        }

        public RouteBuilder<TRequest, TResponse> Mode(RouterModeType mode)
        {
            _mode = mode;
            return this;
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

        public IParticipant<TRequest, TResponse> BuildParticipant()
        {
            var listener = new ScatterRouter<TRequest, TResponse>(_messageBus, _routes, _defaultRoute, _mode);
            return listener;
        }
    }
}