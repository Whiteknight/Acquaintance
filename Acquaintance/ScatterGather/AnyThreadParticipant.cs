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
        private readonly IWorkerPool _workerPool;
        private readonly string _name;

        public AnyThreadParticipant(IParticipantReference<TRequest, TResponse> func, IWorkerPool workerPool, string name)
        {
            _func = func;
            _workerPool = workerPool;
            _name = name;
        }

        public Guid Id { get; set; }
        public string Name => string.IsNullOrEmpty(_name) ? Id.ToString() : _name;
        public bool ShouldStopParticipating => !_func.IsAlive;

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _func.IsAlive;
        }

        public void Scatter(Envelope<TRequest> request, IGatherReceiver<TResponse> scatter)
        {
            var thread = _workerPool.GetFreeWorkerDispatcher();
            if (thread == null)
            {
                ImmediateParticipant<TRequest, TResponse>.GetResponses(Id, _func, request, scatter, Name);
                return;
            }

            var responseWaiter = new DispatchableScatter<TRequest, TResponse>(_func, request, Id, Name, scatter);
            thread.DispatchAction(responseWaiter);
        }
    }
}