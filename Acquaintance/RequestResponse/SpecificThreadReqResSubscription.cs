using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public class SpecificThreadReqResSubscription<TRequest, TResponse> : IReqResSubscription<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly Func<TRequest, TResponse> _func;
        private readonly Func<TRequest, bool> _filter;
        private readonly int _threadId;
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly int _timeoutMs;

        public SpecificThreadReqResSubscription(Func<TRequest, TResponse> func, Func<TRequest, bool> filter, int threadId, MessagingWorkerThreadPool threadPool, int timeoutMs)
        {
            _func = func;
            _filter = filter;
            _threadId = threadId;
            _threadPool = threadPool;
            _timeoutMs = timeoutMs;
        }

        public bool CanHandle(TRequest request)
        {
            return _filter == null || _filter(request);
        }

        public TResponse Request(TRequest request)
        {
            var thread = _threadPool.GetThread(_threadId, false);
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