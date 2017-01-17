using System;

namespace Acquaintance.PubSub
{
    public class ImmediatePubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;

        public ImmediatePubSubSubscription(ISubscriberReference<TPayload> action)
        {
            if (action == null)
                throw new System.ArgumentNullException(nameof(action));

            _action = action;
        }

        public Guid Id { get; set; }

        public void Publish(TPayload payload)
        {
            _action.Invoke(payload);
        }

        public bool ShouldUnsubscribe => !_action.IsAlive;
    }
}