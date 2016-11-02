using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class ReqResChannel<TRequest, TResponse> : IReqResChannel<TRequest, TResponse>
    {
        private readonly ConcurrentDictionary<Guid, IListener<TRequest, TResponse>> _listeners;
        private bool _isExclusive;

        public ReqResChannel()
        {
            _listeners = new ConcurrentDictionary<Guid, IListener<TRequest, TResponse>>();
            _isExclusive = false;
        }

        public IEnumerable<IDispatchableRequest<TResponse>> Request(TRequest request)
        {
            List<IDispatchableRequest<TResponse>> waiters = new List<IDispatchableRequest<TResponse>>();
            foreach (var subscription in _listeners.Values.Where(s => s.CanHandle(request)))
            {
                // TODO: We should order these so worker thread requests are dispatched first, followed by
                // immediate requests.
                var responseWaiter = subscription.Request(request);
                waiters.Add(responseWaiter);
            }
            return waiters;
        }

        public SubscriptionToken Listen(IListener<TRequest, TResponse> listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            Guid id = Guid.NewGuid();
            _listeners.TryAdd(id, listener);
            return new SubscriptionToken(this, id);
        }

        public void Unsubscribe(Guid id)
        {
            IListener<TRequest, TResponse> subscription;
            _listeners.TryRemove(id, out subscription);

            if (_isExclusive && _listeners.IsEmpty)
                _isExclusive = false;
        }

        public void Dispose()
        {
            _listeners.Clear();
        }
    }
}