using System;
using Acquaintance.Threading;

namespace Acquaintance.RequestResponse
{
    public class ThreadPoolListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly IWorkerPool _workerPool;

        public ThreadPoolListener(IListenerReference<TRequest, TResponse> func, IWorkerPool workerPool)
        {
            _func = func;
            _workerPool = workerPool;
        }

        public bool ShouldStopListening => !_func.IsAlive;

        public Guid Id { get; set; }

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _func.IsAlive;
        }

        public void Request(Envelope<TRequest> envelope, IResponseReceiver<TResponse> request)
        {
            var thread = _workerPool.GetThreadPoolDispatcher();
            var responseWaiter = new DispatchableRequest<TRequest, TResponse>(_func, envelope, Id, request);
            thread.DispatchAction(responseWaiter); 
        }   
    }
}