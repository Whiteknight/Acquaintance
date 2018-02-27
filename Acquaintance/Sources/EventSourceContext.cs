using System;
using Acquaintance.Logging;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Sources
{
    public class EventSourceContext : IEventSourceContext
    {
        private readonly IPubSubBus _messageBus;

        public EventSourceContext(IPubSubBus messageBus)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            _messageBus = messageBus;
            IterationDelayMs = -1;
        }

        public Guid Id => _messageBus.Id;
        public ILogger Logger => _messageBus.Logger;
        public int IterationDelayMs { get; set; }

        public IEnvelopeFactory EnvelopeFactory => _messageBus.EnvelopeFactory;

        public bool IsComplete { get; private set; }

        public IWorkerPool WorkerPool => _messageBus.WorkerPool;

        public void Complete()
        {
            IsComplete = true;
        }

        public void PublishEnvelope<TPayload>(Envelope<TPayload> envelope)
        {
            _messageBus.PublishEnvelope(envelope);
        }
    }
}
