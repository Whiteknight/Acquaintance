using Acquaintance.Threading;

namespace Acquaintance.RequestResponse
{
    public class SpecificThreadListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly int _threadId;
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly int _timeoutMs;

        public SpecificThreadListener(IListenerReference<TRequest, TResponse> func, int threadId, MessagingWorkerThreadPool threadPool, int timeoutMs)
        {
            _func = func;
            _threadId = threadId;
            _threadPool = threadPool;
            _timeoutMs = timeoutMs;
        }

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public IDispatchableRequest<TResponse> Request(TRequest request)
        {
            var thread = _threadPool.GetThreadDispatcher(_threadId, false);
            if (thread == null)
                return new ImmediateResponse<TResponse>(default(TResponse));

            var responseWaiter = new DispatchableRequest<TRequest, TResponse>(_func, request, _timeoutMs);
            thread.DispatchAction(responseWaiter);
            return responseWaiter;
        }
    }
}