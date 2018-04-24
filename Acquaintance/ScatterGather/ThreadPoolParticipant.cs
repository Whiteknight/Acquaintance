using System;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class ThreadPoolParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IWorkerPool _workerPool;
        private readonly IParticipantReference<TRequest, TResponse> _action;
        private readonly string _name;

        public ThreadPoolParticipant(IWorkerPool workerPool, IParticipantReference<TRequest, TResponse> action, string name)
        {
            Assert.ArgumentNotNull(workerPool, nameof(workerPool));
            Assert.ArgumentNotNull(action, nameof(action));

            _workerPool = workerPool;
            _action = action;
            _name = name;
        }

        public bool ShouldStopParticipating => !_action.IsAlive;
        public Guid Id { get; set; }
        public string Name => string.IsNullOrEmpty(_name) ? Id.ToString() : _name;

        public bool CanHandle(Envelope<TRequest> request)
        {
            return true;
        }

        public void Scatter(Envelope<TRequest> request, IGatherReceiver<TResponse> scatter)
        {
            var action = new DispatchableScatter<TRequest, TResponse>(_action, request, Id, Name, scatter);
            var context = _workerPool.GetThreadPoolDispatcher();
            context.DispatchAction(action);
        }
    }
}