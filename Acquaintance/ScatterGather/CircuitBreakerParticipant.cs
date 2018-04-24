using System;
using Acquaintance.Utility;

namespace Acquaintance.ScatterGather
{
    public class CircuitBreakerParticipant<TRequest, TResponse> : IParticipant<TRequest, TResponse>
    {
        private readonly IParticipant<TRequest, TResponse> _inner;
        private readonly ICircuitBreaker _circuitBreaker;

        public CircuitBreakerParticipant(IParticipant<TRequest, TResponse> inner, ICircuitBreaker circuitBreaker)
        {
            _inner = inner;
            _circuitBreaker = circuitBreaker;
        }

        public bool ShouldStopParticipating => _inner.ShouldStopParticipating;
        public Guid Id
        {
            get => _inner.Id;
            set => _inner.Id = value;
        }
        public string Name => _inner.Name;

        public bool CanHandle(Envelope<TRequest> request)
        {
            return _inner.CanHandle(request) && _circuitBreaker.CanProceed();
        }

        public void Scatter(Envelope<TRequest> request, IGatherReceiver<TResponse> scatter)
        {
            if (!_circuitBreaker.CanProceed())
            {
                scatter.AddResponse(Id, ScatterResponse<TResponse>.NoResponse(Id, Name));
                return;
            }
            scatter = new Receiver(_circuitBreaker, scatter);
            _inner.Scatter(request, scatter);
        }

        private class Receiver : IGatherReceiver<TResponse>
        {
            private readonly ICircuitBreaker _circuitBreaker;
            private readonly IGatherReceiver<TResponse> _inner;

            public Receiver(ICircuitBreaker circuitBreaker, IGatherReceiver<TResponse> inner )
            {
                _circuitBreaker = circuitBreaker;
                _inner = inner;
            }

            public void AddResponse(Guid participantId, ScatterResponse<TResponse> response)
            {
                _circuitBreaker.RecordResult(response.IsSuccess);
                _inner.AddResponse(participantId, response);
            }

            public void AddParticipant(Guid participantId)
            {
                _inner.AddParticipant(participantId);
            }
        }
    }
}
