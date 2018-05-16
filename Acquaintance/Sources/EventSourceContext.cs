﻿using System;
using Acquaintance.Logging;
using Acquaintance.Modules;
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

        public string Id => _messageBus.Id;
        public ILogger Logger => _messageBus.Logger;

        // TODO: Should we implement this?
        public IEnvelopeFactory EnvelopeFactory => _messageBus.EnvelopeFactory;

        public IModuleManager Modules => throw new NotImplementedException();
        public IWorkerPool WorkerPool => _messageBus.WorkerPool;

        public void PublishEnvelope<TPayload>(Envelope<TPayload> envelope)
        {
            _messageBus.PublishEnvelope(envelope);
        }
    }
}
