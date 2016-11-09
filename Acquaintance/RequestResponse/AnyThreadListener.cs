using Acquaintance.Threading;

namespace Acquaintance.RequestResponse
{
    public class AnyThreadListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly int _timeoutMs;

        public AnyThreadListener(IListenerReference<TRequest, TResponse> func, MessagingWorkerThreadPool threadPool, int timeoutMs)
        {
            _func = func;
            _threadPool = threadPool;
            _timeoutMs = timeoutMs;
        }

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            var thread = _threadPool.GetFreeWorkerThreadDispatcher();
            if (thread == null)
                return new ImmediateResponse<TResponse>(_func.Invoke(request));

            var responseWaiter = new DispatchableRequest<TRequest, TResponse>(_func, request, _timeoutMs);
            thread.DispatchAction(responseWaiter);
            return responseWaiter;
        }
    }
}