using Acquaintance.Threading;
using System;

namespace Acquaintance.ScatterGather
{
    public class SpecificThreadParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly int _threadId;
        private readonly IThreadPool _threadPool;

        public SpecificThreadParticipant(IParticipantReference<TRequest, TResponse> func, int threadId, IThreadPool threadPool)
        {
            _func = func;
            _threadId = threadId;
            _threadPool = threadPool;
        }

        public Guid Id { get; set; }
        public bool ShouldStopParticipating => !_func.IsAlive;

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public void Scatter(TRequest request, ScatterRequest<TResponse> scatter)
        {
            var thread = _threadPool.GetThreadDispatcher(_threadId, false);
            if (thread == null)
            {
                ImmediateParticipant<TRequest, TResponse>.GetResponses(Id, _func, request, scatter);
                return;
            }

            var responseWaiter = new DispatchableScatter<TRequest, TResponse>(_func, request, Id, scatter);
            thread.DispatchAction(responseWaiter);
        }
    }
}