using System;
using Acquaintance.Threading;

namespace Acquaintance.RequestResponse
{
    public class AnyThreadReqResSubscription<TRequest, TResponse> : IReqResSubscription<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly Func<TRequest, TResponse> _func;
        private readonly Func<TRequest, bool> _filter;
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly int _timeoutMs;

        public AnyThreadReqResSubscription(Func<TRequest, TResponse> func, Func<TRequest, bool> filter, MessagingWorkerThreadPool threadPool, int timeoutMs)
        {
            _func = func;
            _filter = filter;
            _threadPool = threadPool;
            _timeoutMs = timeoutMs;
        }

        public bool CanHandle(TRequest request)
        {
            return _filter == null || _filter(request);
        }

        public TResponse Request(TRequest request)
        {
            var thread = _threadPool.GetAnyThread();
            if (thread == null)
                return default(TResponse);
            var responseWaiter = new DispatchableRequest<TRequest, TResponse>(_func, request, _timeoutMs);
            thread.DispatchAction(responseWaiter);
            bool complete = responseWaiter.WaitForResponse();
            if (!complete)
                return default(TResponse);
            return responseWaiter.Response;
        }
    }
}