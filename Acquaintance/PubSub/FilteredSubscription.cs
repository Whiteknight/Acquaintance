using System;

namespace Acquaintance.PubSub
{
    public class FilteredSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscription<TPayload> _inner;
        private readonly Func<TPayload, bool> _filter;

        public FilteredSubscription(ISubscription<TPayload> inner, Func<TPayload, bool> filter)
        {
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            _inner = inner;
            _filter = filter;
        }

        public void Publish(TPayload payload)
        {
            if (!_filter(payload))
                return;

            _inner.Publish(payload);
        }
    }
}