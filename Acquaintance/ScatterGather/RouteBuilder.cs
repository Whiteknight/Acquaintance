﻿using Acquaintance.Common;
using System;
using System.Collections.Generic;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class RouteBuilder<TRequest, TResponse>
    {
        private readonly IScatterGatherBus _messageBus;
        private readonly List<EventRoute<TRequest>> _routes;
        private string _defaultRoute;
        private RouterModeType _mode;

        public RouteBuilder(IScatterGatherBus messageBus)
        {
            _messageBus = messageBus;
            _routes = new List<EventRoute<TRequest>>();
        }

        public RouteBuilder<TRequest, TResponse> WithMode(RouterModeType mode)
        {
            _mode = mode;
            return this;
        }

        public RouteBuilder<TRequest, TResponse> When(Func<TRequest, bool> predicate, string topic)
        {
            Assert.ArgumentNotNull(predicate, nameof(predicate));

            _routes.Add(new EventRoute<TRequest>(topic, predicate));
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
            return new ScatterRouter<TRequest, TResponse>(_messageBus, _routes, _defaultRoute, _mode);
        }
    }
}