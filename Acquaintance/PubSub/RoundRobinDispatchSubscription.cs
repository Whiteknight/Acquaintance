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
        private readonly IPublishable _messageBus;

        public RoundRobinDispatchSubscription(IPublishable messageBus, IEnumerable<string> channels)
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

        public bool ShouldUnsubscribe => false;

        public void Publish(TPayload payload)
        {
            int idx = Interlocked.Increment(ref _idx);
            idx = (idx + 1) % _channels.Length;

            string channel = _channels[idx];
            _messageBus.Publish(channel, payload);
        }
    }
}
