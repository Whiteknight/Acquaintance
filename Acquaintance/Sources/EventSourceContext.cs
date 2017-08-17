using Acquaintance.PubSub;
using Acquaintance.Threading;
using System;
using Acquaintance.Routing;
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

        public int IterationDelayMs { get; set; }

        public IEnvelopeFactory EnvelopeFactory => _messageBus.EnvelopeFactory;

        public bool IsComplete { get; private set; }

        public IThreadPool ThreadPool => _messageBus.ThreadPool;

        public IPublishTopicRouter PublishRouter => _messageBus.PublishRouter;

        public void Complete()
        {
            IsComplete = true;
        }

        public void PublishEnvelope<TPayload>(Envelope<TPayload> envelope)
        {
            _messageBus.PublishEnvelope(envelope);
        }

        public IDisposable Subscribe<TPayload>(string topic, ISubscription<TPayload> subscription)
        {
            return null;
        }
    }
}
