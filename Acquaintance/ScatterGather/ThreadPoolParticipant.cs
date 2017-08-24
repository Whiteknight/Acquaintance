using System;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class ThreadPoolParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IWorkerPool _workerPool;
        private readonly IParticipantReference<TRequest, TResponse> _action;

        public ThreadPoolParticipant(IWorkerPool workerPool, IParticipantReference<TRequest, TResponse> action)
        {
            Assert.ArgumentNotNull(workerPool, nameof(workerPool));
            Assert.ArgumentNotNull(action, nameof(action));

            _workerPool = workerPool;
            _action = action;
        }

        public bool ShouldStopParticipating => !_action.IsAlive;
        public Guid Id { get; set; }

        public bool CanHandle(Envelope<TRequest> request)
        {
            return true;
        }

        public void Scatter(Envelope<TRequest> request, IGatherReceiver<TResponse> scatter)
        {
            var action = new DispatchableScatter<TRequest, TResponse>(_action, request.Payload, Id, scatter);
            var context = _workerPool.GetThreadPoolDispatcher();
            context.DispatchAction(action);
        }
    }
}