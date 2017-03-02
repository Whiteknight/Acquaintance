using System;

namespace Acquaintance.Sources
{
    public class EventSourceContext : IEventSourceContext
    {
        private readonly IPublishable _messageBus;

        public EventSourceContext(IPublishable messageBus)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            _messageBus = messageBus;
        }

        public bool IsComplete { get; private set; }

        public void Complete()
        {
            IsComplete = true;
        }

        public void Publish<TPayload>(string channelName, TPayload payload)
        {
            _messageBus.Publish<TPayload>(channelName, payload);
        }
    }
}
