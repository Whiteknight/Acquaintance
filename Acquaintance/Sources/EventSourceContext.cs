using System;
using Acquaintance.Logging;
using Acquaintance.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Sources
{
    public class EventSourceContext : IntervalWorkerContext, IEventSourceContext
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
       
        public IEnvelopeFactory EnvelopeFactory => _messageBus.EnvelopeFactory;

        public IWorkerPool WorkerPool => _messageBus.WorkerPool;

        public void PublishEnvelope<TPayload>(Envelope<TPayload> envelope)
        {
            _messageBus.PublishEnvelope(envelope);
        }
    }
}
