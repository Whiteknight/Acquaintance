using Acquaintance.PubSub;
using Acquaintance.Threading;
using System;

namespace Acquaintance.Sources
{
    public class EventSourceContext : IEventSourceContext
    {
        private readonly IPubSubBus _messageBus;

        public EventSourceContext(IPubSubBus messageBus)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            _messageBus = messageBus;
        }

        public IEnvelopeFactory EnvelopeFactory => _messageBus.EnvelopeFactory;

        public bool IsComplete { get; private set; }

        public IThreadPool ThreadPool => _messageBus.ThreadPool;

        public void Complete()
        {
            IsComplete = true;
        }

        public void PublishEnvelope<TPayload>(Envelope<TPayload> envelope)
        {
            _messageBus.PublishEnvelope<TPayload>(envelope);
        }

        public IDisposable Subscribe<TPayload>(string channelName, ISubscription<TPayload> subscription)
        {
            return null;
        }
    }
}
