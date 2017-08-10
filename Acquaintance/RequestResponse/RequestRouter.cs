using Acquaintance.Common;
using System;
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

        public Guid Id { get; set; }

        public bool CanHandle(Envelope<TRequest> request)
        {
            // TODO: Add Filtering
            return true;
        }

        public IDispatchableRequest<TResponse> Request(Envelope<TRequest> request)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(request.Payload));
            if (route == null)
            {
                if (_defaultRouteOrNull != null)
                {
                    request = request.RedirectToTopic(_defaultRouteOrNull);
                    var response1 = _messageBus.RequestEnvelope<TRequest, TResponse>(request);
                    return new ImmediateResponse<TResponse>(Id, response1);
                }
                return new ImmediateResponse<TResponse>(Id, default(TResponse));
            }

            request = request.RedirectToTopic(route.Topic);
            var response = _messageBus.RequestEnvelope<TRequest, TResponse>(request);
            return new ImmediateResponse<TResponse>(Id, response);
        }

        public bool ShouldStopListening => false;
    }
}
