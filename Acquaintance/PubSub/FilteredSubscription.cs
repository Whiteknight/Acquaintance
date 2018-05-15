using System;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class FilteredSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscription<TPayload> _inner;
        private readonly Func<Envelope<TPayload>, bool> _filter;

        public FilteredSubscription(ISubscription<TPayload> inner, Func<Envelope<TPayload>, bool> filter)
        {
            Assert.ArgumentNotNull(inner, nameof(inner));
            Assert.ArgumentNotNull(filter, nameof(filter));

            _inner = inner;
            _filter = filter;
        }

        public static ISubscription<TPayload> WrapSubscription(ISubscription<TPayload> inner, Func<Envelope<TPayload>, bool> filter)
        {
            if (filter == null)
                return inner;
            return new FilteredSubscription<TPayload>(inner, filter);
        }

        public Guid Id
        {
            get => _inner.Id;
            set => _inner.Id = value;
        }

        public void Publish(Envelope<TPayload> message)
        {
            if (!_filter(message))
                return;

            _inner.Publish(message);
        }

        public bool ShouldUnsubscribe => _inner.ShouldUnsubscribe;

        public void Dispose()
        {
            _inner?.Dispose();
        }
    }
}