using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class FilteredSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscription<TPayload> _inner;
        private readonly Func<TPayload, bool> _filter;

        // TODO: Ability to filter on Envelope metadata?
        public FilteredSubscription(ISubscription<TPayload> inner, Func<TPayload, bool> filter)
        {
            Assert.ArgumentNotNull(inner, nameof(inner));
            Assert.ArgumentNotNull(filter, nameof(filter));

            _inner = inner;
            _filter = filter;
        }

        public Guid Id
        {
            get { return _inner.Id; }
            set { _inner.Id = value; }
        }

        public void Publish(Envelope<TPayload> message)
        {
            if (!_filter(message.Payload))
                return;

            _inner.Publish(message);
        }

        public bool ShouldUnsubscribe => _inner.ShouldUnsubscribe;
    }
}