using Acquaintance.Threading;
using System;

namespace Acquaintance.RequestResponse
{
    public class SpecificThreadListener<TRequest, TResponse> : IListener<TRequest, TResponse>
    {
        private readonly IListenerReference<TRequest, TResponse> _func;
        private readonly int _threadId;
        private readonly IThreadPool _threadPool;

        public SpecificThreadListener(IListenerReference<TRequest, TResponse> func, int threadId, IThreadPool threadPool)
        {
            _func = func;
            _threadId = threadId;
            _threadPool = threadPool;
        }

        public Guid Id { get; set; }

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _func.IsAlive;
        }

        public void Request(Envelope<TRequest> envelope, IResponseReceiver<TResponse> request)
        {
            var thread = _threadPool.GetThreadDispatcher(_threadId, false);
            if (thread == null)
            {
                request.SetNoResponse();
                return;
            }

            var responseWaiter = new DispatchableRequest<TRequest, TResponse>(_func, envelope, Id, request);
            thread.DispatchAction(responseWaiter);
        }

        public bool ShouldStopListening => !_func.IsAlive;
    }
}