using Acquaintance.Threading;
using System;

namespace Acquaintance.ScatterGather
{
    public class SpecificThreadParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly int _threadId;
        private readonly IThreadPool _threadPool;
        private readonly int _timeoutMs;

        public SpecificThreadParticipant(IParticipantReference<TRequest, TResponse> func, int threadId, IThreadPool threadPool, int timeoutMs)
        {
            _func = func;
            _threadId = threadId;
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
            var thread = _threadPool.GetThreadDispatcher(_threadId, false);
            if (thread == null)
                return new ImmediateGather<TResponse>(Id, null);

            var responseWaiter = new DispatchableScatter<TRequest, TResponse>(_func, request, Id, _timeoutMs);
            thread.DispatchAction(responseWaiter);
            return responseWaiter;
        }
    }
}