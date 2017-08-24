using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public class AnyThreadListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly IThreadPool _threadPool;

        public AnyThreadListener(IListenerReference<TRequest, TResponse> func, IThreadPool threadPool)
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

        public void Request(Envelope<TRequest> envelope, IResponseReceiver<TResponse> request)
        {
            var thread = _threadPool.GetAnyThreadDispatcher();
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