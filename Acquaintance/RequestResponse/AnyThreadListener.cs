using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public class AnyThreadListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly IWorkerPool _workerPool;

        public AnyThreadListener(IListenerReference<TRequest, TResponse> func, IWorkerPool workerPool)
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
            var thread = _workerPool.GetAnyWorkerDispatcher();
            if (thread == null)
            {
                var response = _func.Invoke(envelope);
                request.SetResponse(response);
                return;
            }

            var responseWaiter = new DispatchableRequest<TRequest, TResponse>(_func, envelope, Id, request);
            thread.DispatchAction(responseWaiter);
        }
    }
}