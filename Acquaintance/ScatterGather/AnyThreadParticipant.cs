using Acquaintance.Threading;
using System;

namespace Acquaintance.ScatterGather
{
    public class AnyThreadParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly IThreadPool _threadPool;
        private readonly int _timeoutMs;

        public AnyThreadParticipant(IParticipantReference<TRequest, TResponse> func, IThreadPool threadPool, int timeoutMs)
        {
            _func = func;
            _threadPool = threadPool;
            _timeoutMs = timeoutMs;
        }
        public Guid Id { get; set; }
        public bool ShouldStopParticipating => !_func.IsAlive;

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public IDispatchableScatter<TResponse> Scatter(TRequest request)
        {
            var thread = _threadPool.GetFreeWorkerThreadDispatcher();
            if (thread == null)
                return new ImmediateGather<TResponse>(Id, _func.Invoke(request));

            var responseWaiter = new DispatchableScatter<TRequest, TResponse>(_func, request, Id, _timeoutMs);
            thread.DispatchAction(responseWaiter);
            return responseWaiter;
        }
    }
}