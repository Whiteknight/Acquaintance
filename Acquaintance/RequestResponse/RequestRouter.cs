using Acquaintance.Common;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class RequestRouter<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IReadOnlyList<EventRoute<TRequest>> _routes;
        private readonly string _defaultRouteOrNull;
        private readonly IReqResBus _messageBus;

        public RequestRouter(IReqResBus messageBus, IReadOnlyList<EventRoute<TRequest>> routes, string defaultRouteOrNull)
        {
            _routes = routes;
            _defaultRouteOrNull = defaultRouteOrNull;
            _messageBus = messageBus;
        }

        public bool CanHandle(TRequest request)
        {
            // TODO: Add Filtering
            return true;
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(request));
            if (route == null)
            {
                if (_defaultRouteOrNull != null)
                {
                    var response1 = _messageBus.Request<TRequest, TResponse>(_defaultRouteOrNull, request);
                    return new ImmediateResponse<TResponse>(response1);
                }
                return new ImmediateResponse<TResponse>(default(TResponse));
            }
            var response = _messageBus.Request<TRequest, TResponse>(route.ChannelName, request);
            return new ImmediateResponse<TResponse>(response);
        }

        public bool ShouldStopListening => false;
    }
}
