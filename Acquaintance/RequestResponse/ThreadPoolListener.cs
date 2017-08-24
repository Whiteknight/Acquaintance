using System;
using Acquaintance.Threading;

namespace Acquaintance.RequestResponse
{
    public class ThreadPoolListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly IThreadPool _threadPool;

        public ThreadPoolListener(IListenerReference<TRequest, TResponse> func, IThreadPool threadPool)
        {
            _func = func;
            _threadPool = threadPool;
        }

        public bool ShouldStopListening => !_func.IsAlive;

        public Guid Id { get; set; }

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _func.IsAlive;
        }

        public void Request(Envelope<TRequest> envelope, Request<TResponse> request)
        {
            var thread = _threadPool.GetThreadPoolActionDispatcher();
            var responseWaiter = new DispatchableRequest<TRequest, TResponse>(_func, envelope, Id, request);
            thread.DispatchAction(responseWaiter); 
        }   
    }
}