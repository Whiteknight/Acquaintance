using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class ReqResChannel<TRequest, TResponse> : IReqResChannel<TRequest, TResponse>
    {
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly Dictionary<Guid, IReqResSubscription<TRequest, TResponse>> _subscriptions;

        public ReqResChannel(MessagingWorkerThreadPool threadPool)
        {
            _threadPool = threadPool;
            _subscriptions = new Dictionary<Guid, IReqResSubscription<TRequest, TResponse>>();
        }

        public void Unsubscribe(Guid id)
        {
            _subscriptions.Remove(id);
        }

        public IEnumerable<TResponse> Request(TRequest request)
        {
            List<IDispatchableRequest<TResponse>> waiters = new List<IDispatchableRequest<TResponse>>();
            foreach (var subscription in _subscriptions.Values.Where(s => s.CanHandle(request)))
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

        public SubscriptionToken Listen(Func<TRequest, TResponse> act, Func<TRequest, bool> filter, SubscribeOptions options)
        {
            Guid id = Guid.NewGuid();
            var subscription = CreateSubscription(act, filter, options);
            _subscriptions.Add(id, subscription);
            return new SubscriptionToken(this, id);
        }

        public void Dispose()
        {
            _subscriptions.Clear();
        }

        private IReqResSubscription<TRequest, TResponse> CreateSubscription(Func<TRequest, TResponse> func, Func<TRequest, bool> filter, SubscribeOptions options)
        {
            switch (options.DispatchType)
            {
                case DispatchThreadType.AnyWorkerThread:
                    return new AnyThreadReqResSubscription<TRequest, TResponse>(func, filter, _threadPool, options.WaitTimeoutMs);
                case DispatchThreadType.SpecificThread:
                    return new SpecificThreadReqResSubscription<TRequest, TResponse>(func, filter, options.ThreadId, _threadPool, options.WaitTimeoutMs);
                default:
                    return new ImmediateReqResSubscription<TRequest, TResponse>(func, filter);
            }
        }
    }
}