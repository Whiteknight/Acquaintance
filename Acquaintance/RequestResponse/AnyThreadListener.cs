using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public class AnyThreadListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly IThreadPool _threadPool;
        private readonly int _timeoutMs;

        public AnyThreadListener(IListenerReference<TRequest, TResponse> func, IThreadPool threadPool, int timeoutMs)
        {
            _func = func;
            _threadPool = threadPool;
            _timeoutMs = timeoutMs;
        }

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _func.IsAlive;
        }

        public IDispatchableRequest<TResponse> Request(Envelope<TRequest> request)
        {
            var thread = _threadPool.GetAnyThreadDispatcher();
            if (thread == null)
                return new ImmediateResponse<TResponse>(Id, _func.Invoke(request));

            var responseWaiter = new DispatchableRequest<TRequest, TResponse>(_func, request, Id, _timeoutMs);
            thread.DispatchAction(responseWaiter);
            return responseWaiter;
        }

        public bool ShouldStopListening => !_func.IsAlive;

        public Guid Id { get; set; }
    }
}