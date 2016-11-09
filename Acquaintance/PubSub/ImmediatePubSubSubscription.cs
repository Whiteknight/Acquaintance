using System;

namespace Acquaintance.PubSub
{
    public class ImmediatePubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly Action<TPayload> _act;

        public ImmediatePubSubSubscription(Action<TPayload> act)
        {
            _act = act;
        }

        public void Publish(TPayload payload)
        {
            _act(payload);
        }

        public bool ShouldUnsubscribe => false;
    }
}