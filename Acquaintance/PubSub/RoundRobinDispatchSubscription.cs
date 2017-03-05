using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Acquaintance.PubSub
{
    public class RoundRobinDispatchSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly string[] _channels;
        private int _idx;
        private readonly IPubSubBus _messageBus;

        public RoundRobinDispatchSubscription(IPubSubBus messageBus, IEnumerable<string> channels)
        {
            if (channels == null)
                throw new ArgumentNullException(nameof(channels));
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));

            _channels = channels.ToArray();
            if (_channels.Length == 0)
                throw new Exception("No channel names provided");
            _idx = 0;
            _messageBus = messageBus;
        }

        public Guid Id { get; set; }
        public bool ShouldUnsubscribe => false;

        public void Publish(Envelope<TPayload> message)
        {
            int idx = Interlocked.Increment(ref _idx);
            idx = idx % _channels.Length;

            string channel = _channels[idx];
            message = message.RedirectToChannel(channel);
            _messageBus.PublishEnvelope(message);
        }
    }
}
