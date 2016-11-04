using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public class AnyThreadListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly Func<TRequest, TResponse> _func;
        private readonly Func<TRequest, bool> _filter;
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly int _timeoutMs;

        public AnyThreadListener(Func<TRequest, TResponse> func, Func<TRequest, bool> filter, MessagingWorkerThreadPool threadPool, int timeoutMs)
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

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            var thread = _threadPool.GetFreeWorkerThreadDispatcher();
            if (thread == null)
                return new ImmediateResponse<TResponse>(_func(request));

            var responseWaiter = new DispatchableRequest<TRequest, TResponse>(_func, request, _timeoutMs);
            thread.DispatchAction(responseWaiter);
            return responseWaiter;
        }
    }
}