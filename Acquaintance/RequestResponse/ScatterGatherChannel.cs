using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Acquaintance.RequestResponse
{
    public class ScatterGatherChannel<TRequest, TResponse> : IReqResChannel<TRequest, TResponse>
    {
        private readonly ConcurrentDictionary<Guid, IListener<TRequest, TResponse>> _listeners;

        public ScatterGatherChannel()
        {
            _listeners = new ConcurrentDictionary<Guid, IListener<TRequest, TResponse>>();
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public IEnumerable<IDispatchableRequest<TResponse>> Request(TRequest request)
        {
            List<IDispatchableRequest<TResponse>> waiters = new List<IDispatchableRequest<TResponse>>();
            List<Guid> toRemove = new List<Guid>();
            foreach (var kvp in _listeners)
            {
                try
                {
                    var listener = kvp.Value;
                    if (!listener.CanHandle(request))
                        continue;

                    // TODO: We should order these so worker thread requests are dispatched first, followed by
                    // immediate requests.
                    var responseWaiter = listener.Request(request);
                    if (listener.ShouldStopListening)
                        toRemove.Add(kvp.Key);
                    waiters.Add(responseWaiter);
                }
                catch { }
            }
            foreach (var id in toRemove)
                Unsubscribe(id);
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
        }

        public void Dispose()
        {
            _listeners.Clear();
        }
    }
}