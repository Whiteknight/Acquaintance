using Acquaintance.Common;
using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
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
            _messageBus = messageBus;
        }

        public IDisposable Token { get; set; }

        public bool CanHandle(TRequest request)
        {
            // TODO: Add Filtering
            return true;
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            switch (_modeType)
            {
                case RouterModeType.FirstMatchingRoute:
                    return RequestFirstOrDefault(request);
                case RouterModeType.AllMatchingRoutes:
                    return RequestAllMatching(request);
            }
            return new ImmediateResponse<TResponse>(null);
        }

        private IDispatchableRequest<TResponse> RequestFirstOrDefault(TRequest request)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(request));
            if (route == null)
            {
                if (_defaultRouteOrNull != null)
                {
                    var response1 = _messageBus.Scatter<TRequest, TResponse>(_defaultRouteOrNull, request);
                    return new ImmediateResponse<TResponse>(response1.Responses.ToArray());
                }
                return new ImmediateResponse<TResponse>(null);
            }
            var response = _messageBus.Scatter<TRequest, TResponse>(route.ChannelName, request);
            return new ImmediateResponse<TResponse>(response.Responses.ToArray());
        }

        private IDispatchableRequest<TResponse> RequestAllMatching(TRequest request)
        {
            var allResponses = Enumerable.Empty<TResponse>();
            foreach (var route in _routes.Where(r => r.Predicate(request)))
            {
                var responses = _messageBus.Scatter<TRequest, TResponse>(request);
                allResponses = allResponses.Concat(responses.Responses);
            }
            return new ImmediateResponse<TResponse>(allResponses.ToArray());
        }

        public bool ShouldStopParticipating => false;
    }
}