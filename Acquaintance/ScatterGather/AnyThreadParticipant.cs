using Acquaintance.Threading;
using System;

namespace Acquaintance.ScatterGather
{
    /// <summary>
    /// Execute the participant on any available thread
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public class AnyThreadParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipantReference<TRequest, TResponse> _func;
        private readonly IThreadPool _threadPool;

        public AnyThreadParticipant(IParticipantReference<TRequest, TResponse> func, IThreadPool threadPool)
        {
            _func = func;
            _threadPool = threadPool;
        }

        public Guid Id { get; set; }
        public bool ShouldStopParticipating => !_func.IsAlive;

        public bool CanHandle(TRequest request)
        {
            return _func.IsAlive;
        }

        public void Scatter(TRequest request, Scatter<TResponse> scatter)
        {
            var thread = _threadPool.GetFreeWorkerThreadDispatcher();
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