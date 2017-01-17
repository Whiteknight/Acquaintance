using Acquaintance.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.ScatterGather
{
    public class ScatterRouter<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IReadOnlyList<EventRoute<TRequest>> _routes;
        private readonly IReqResBus _messageBus;
        private readonly string _defaultRouteOrNull;
        private readonly RouterModeType _modeType;

        public ScatterRouter(IReqResBus messageBus, IReadOnlyList<EventRoute<TRequest>> routes, string defaultRouteOrNull, RouterModeType modeType)
        {
            _routes = routes;
            _defaultRouteOrNull = defaultRouteOrNull;
            _modeType = modeType;
            _messageBus = messageBus;
        }

        public Guid Id { get; set; }
        public IDisposable Token { get; set; }
        public bool ShouldStopParticipating => false;

        public bool CanHandle(TRequest request)
        {
            // TODO: Add Filtering
            return true;
        }

        public IDispatchableScatter<TResponse> Scatter(TRequest request)
        {
            switch (_modeType)
            {
                case RouterModeType.FirstMatchingRoute:
                    return ScatterFirstOrDefault(request);
                case RouterModeType.AllMatchingRoutes:
                    return ScatterAllMatching(request);
            }
            return new ImmediateGather<TResponse>(Id, null);
        }

        private IDispatchableScatter<TResponse> ScatterFirstOrDefault(TRequest request)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(request));
            if (route == null)
            {
                if (_defaultRouteOrNull != null)
                {
                    var response1 = _messageBus.Scatter<TRequest, TResponse>(_defaultRouteOrNull, request);
                    return new ImmediateGather<TResponse>(Id, response1.ToArray());
                }
                return new ImmediateGather<TResponse>(Id, null);
            }
            var response = _messageBus.Scatter<TRequest, TResponse>(route.ChannelName, request);
            return new ImmediateGather<TResponse>(Id, response.ToArray());
        }

        private IDispatchableScatter<TResponse> ScatterAllMatching(TRequest request)
        {
            var allResponses = Enumerable.Empty<TResponse>();
            foreach (var route in _routes.Where(r => r.Predicate(request)))
            {
                var responses = _messageBus.Scatter<TRequest, TResponse>(route.ChannelName, request);
                allResponses = allResponses.Concat(responses.AsEnumerable());
            }
            return new ImmediateGather<TResponse>(Id, allResponses.ToArray());
        }
    }
}