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

        public void Request(Envelope<TRequest> envelope, Request<TResponse> request)
        {
            var route = _routes.FirstOrDefault(r => r.Predicate(envelope.Payload));
            if (route == null)
            {
                if (_defaultRouteOrNull != null)
                {
                    envelope = envelope.RedirectToTopic(_defaultRouteOrNull);
                    var response1 = _messageBus.RequestEnvelope<TRequest, TResponse>(envelope);
                    if (response1.WaitForResponse())
                        request.SetResponse(response1.GetResponse());
                    else
                        request.SetNoResponse();
                    return;
                }
                request.SetNoResponse();
                return;
            }

            envelope = envelope.RedirectToTopic(route.Topic);
            var response = _messageBus.RequestEnvelope<TRequest, TResponse>(envelope);
            if (response.WaitForResponse())
                request.SetResponse(response.GetResponse());
            else
                request.SetNoResponse();
        }

        public bool ShouldStopListening => false;
    }
}
