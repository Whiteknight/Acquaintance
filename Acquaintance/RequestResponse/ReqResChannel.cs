using Acquaintance.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class ReqResChannel<TRequest, TResponse> : IReqResChannel<TRequest, TResponse>
    {
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly ConcurrentDictionary<Guid, IListener<TRequest, TResponse>> _listeners;

        public ReqResChannel(MessagingWorkerThreadPool threadPool)
        {
            _threadPool = threadPool;
            _listeners = new ConcurrentDictionary<Guid, IListener<TRequest, TResponse>>();
        }

        public void Unsubscribe(Guid id)
        {
            IListener<TRequest, TResponse> subscription;
            _listeners.TryRemove(id, out subscription);
        }

        public IEnumerable<TResponse> Request(TRequest request)
        {
            List<IDispatchableRequest<TResponse>> waiters = new List<IDispatchableRequest<TResponse>>();
            foreach (var subscription in _listeners.Values.Where(s => s.CanHandle(request)))
            {
                // TODO: We should order these so worker thread requests are dispatched first, followed by
                // immediate requests.
                var responseWaiter = subscription.Request(request);
                waiters.Add(responseWaiter);
            }
            List<TResponse> responses = new List<TResponse>();
            foreach (var waiter in waiters)
            {
                bool complete = waiter.WaitForResponse();
                if (!complete)
                    responses.Add(default(TResponse));
                else
                    responses.Add(waiter.Response);
                waiter.Dispose();
            }

            return responses;
        }

        public SubscriptionToken Listen(IListener<TRequest, TResponse> listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            Guid id = Guid.NewGuid();
            _listeners.TryAdd(id, listener);
            return new SubscriptionToken(this, id);
        }

        public void Dispose()
        {
            _listeners.Clear();
        }
    }
}