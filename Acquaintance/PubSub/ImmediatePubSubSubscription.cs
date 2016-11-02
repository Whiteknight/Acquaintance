using System;

namespace Acquaintance.PubSub
{
    public class ImmediatePubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly Action<TPayload> _act;
        private readonly Func<TPayload, bool> _filter;

        public ImmediatePubSubSubscription(Action<TPayload> act, Func<TPayload, bool> filter)
        {
            _act = act;
            _filter = filter;
        }

        public void Publish(TPayload payload)
        {
            if (_filter != null && !_filter(payload))
                return;
            _act(payload);
        }
    }
}