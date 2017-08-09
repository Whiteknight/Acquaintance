using System;

namespace Acquaintance.RequestResponse
{
    public class RequestResponseChannel<TRequest, TResponse> : IReqResChannel<TRequest, TResponse>
    {
        private Guid _tokenId;
        private IListener<TRequest, TResponse> _listener;

        public RequestResponseChannel()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public IDispatchableRequest<TResponse> Request(Envelope<TRequest> request)
        {
            var listener = _listener;
            if (listener == null || !listener.CanHandle(request))
                return new ImmediateResponse<TResponse>(Id, default(TResponse));

            try
            {
                var waiter = listener.Request(request);
                if (listener.ShouldStopListening)
                    _listener = null;

                return waiter;
            }
            catch
            {
                return new ImmediateResponse<TResponse>(Id, default(TResponse));
            }
        }

        public SubscriptionToken Listen(IListener<TRequest, TResponse> listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            var existing = System.Threading.Interlocked.CompareExchange(ref _listener, listener, null);
            if (existing != null)
                throw new Exception("Cannot register a second Listener on this channel");

            _tokenId = Guid.NewGuid();
            return new SubscriptionToken(this, _tokenId);
        }

        public void Unsubscribe(Guid id)
        {
            if (id == _tokenId)
                _listener = null;
        }

        public void Dispose()
        {
            _listener = null;
        }
    }
}