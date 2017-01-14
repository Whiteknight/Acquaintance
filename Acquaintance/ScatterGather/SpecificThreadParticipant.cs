using Acquaintance.Threading;

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

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public IDispatchableScatter<TResponse> Scatter(TRequest request)
        {
            var thread = _threadPool.GetThreadDispatcher(_threadId, false);
            if (thread == null)
                return new ImmediateGather<TResponse>(null);

            var responseWaiter = new DispatchableScatter<TRequest, TResponse>(_func, request, _timeoutMs);
            thread.DispatchAction(responseWaiter);
            return responseWaiter;
        }

        public bool ShouldStopParticipating => !_func.IsAlive;
    }
}