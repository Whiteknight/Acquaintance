using System.Threading;

namespace Acquaintance.PubSub
{
    public class MaxEventsSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscription<TPayload> _inner;
        private int _maxEvents;

        public MaxEventsSubscription(ISubscription<TPayload> inner, int maxEvents)
        {
            _inner = inner;
            _maxEvents = maxEvents;
        }

        public void Publish(TPayload payload)
        {
            int maxEvents = Interlocked.Decrement(ref _maxEvents);
            if (maxEvents >= 0)
                _inner.Publish(payload);
        }

        public bool ShouldUnsubscribe
        {
            get { return _inner.ShouldUnsubscribe || _maxEvents <= 0; }
        }
    }
}