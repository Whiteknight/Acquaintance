using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class RoundRobinDispatchSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly string[] _topics;
        private int _idx;
        private readonly IPubSubBus _messageBus;

        public RoundRobinDispatchSubscription(IPubSubBus messageBus, IEnumerable<string> topics)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(topics, nameof(topics));

            _topics = topics.ToArray();
            if (_topics.Length == 0)
                throw new Exception("No channel names provided");
            _idx = 0;
            _messageBus = messageBus;
        }

        public Guid Id { get; set; }
        public bool ShouldUnsubscribe => false;

        public void Publish(Envelope<TPayload> message)
        {
            int idx = Interlocked.Increment(ref _idx);
            idx = idx % _topics.Length;

            string topic = _topics[idx];
            message = message.RedirectToTopic(topic);
            _messageBus.PublishEnvelope(message);
        }
    }
}
