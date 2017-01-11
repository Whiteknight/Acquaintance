using Acquaintance.PubSub;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class RequestRouter<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IReadOnlyList<EventRoute<TRequest>> _routes;
        private readonly IReqResBus _messageBus;
        private readonly IListenerReference<TRequest, TResponse> _defaultFunc;

        public RequestRouter(IReqResBus messageBus, IReadOnlyList<EventRoute<TRequest>> routes, IListenerReference<TRequest, TResponse> defaultFunc)
        {
            _routes = routes;
            _messageBus = messageBus;
            _defaultFunc = defaultFunc;
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
                if (_defaultFunc != null)
                {
                    var responses = _defaultFunc.Invoke(request);
                    return new ImmediateResponse<TResponse>(responses);
                }
                return new ImmediateResponse<TResponse>(null);
            }
            var response = _messageBus.Request<TRequest, TResponse>(route.ChannelName, request);
            return new ImmediateResponse<TResponse>(new[] { response });
        }

        public bool ShouldStopListening => false;
    }
}
