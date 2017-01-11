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

        public ScatterRouter(IReqResBus messageBus, IReadOnlyList<EventRoute<TRequest>> routes)
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
            var routes = _routes.Where(r => r.Predicate(request));
            List<TResponse> responses = new List<TResponse>();
            foreach (var route in routes)
            {
                var response = _messageBus.Scatter<TRequest, TResponse>(route.ChannelName, request);
                responses.AddRange(response.ToArray());
            }
            return new ImmediateResponse<TResponse>(responses.ToArray());
        }

        public bool ShouldStopParticipating => false;
    }
}