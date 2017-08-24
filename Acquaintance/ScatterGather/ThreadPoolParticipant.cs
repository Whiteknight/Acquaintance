using System;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class ThreadPoolParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IThreadPool _threadPool;
        private readonly IParticipantReference<TRequest, TResponse> _action;

        public ThreadPoolParticipant(IThreadPool threadPool, IParticipantReference<TRequest, TResponse> action)
        {
            Assert.ArgumentNotNull(threadPool, nameof(threadPool));
            Assert.ArgumentNotNull(action, nameof(action));

            _threadPool = threadPool;
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
            var context = _threadPool.GetThreadPoolActionDispatcher();
            context.DispatchAction(action);
        }
    }
}