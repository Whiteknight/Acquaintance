using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Acquaintance.RequestResponse
{
    public class ExclusiveReqResChannel<TRequest, TResponse> : IReqResChannel<TRequest, TResponse>
    {
        private Guid _tokenId;
        private IListener<TRequest, TResponse> _listener;

        public IEnumerable<IDispatchableRequest<TResponse>> Request(TRequest request)
        {
            var listener = _listener;
            if (listener == null)
                return Enumerable.Empty<IDispatchableRequest<TResponse>>();

            IDispatchableRequest<TResponse> response = listener.Request(request);
            return new List<IDispatchableRequest<TResponse>>
            {
                response
            };
        }

        public SubscriptionToken Listen(IListener<TRequest, TResponse> listener)
        {
            var exchanged = Interlocked.CompareExchange(ref _listener, listener, null);
            if (exchanged != null)
                throw new Exception("Cannot add a second listener to an exclusive channel");
            _tokenId = Guid.NewGuid();
            return new SubscriptionToken(this, _tokenId);
        }

        public void Unsubscribe(Guid id)
        {
            if (_tokenId != id)
                return;
            _listener = null;
        }

        public void Dispose()
        {
            _listener = null;
        }
    }
}