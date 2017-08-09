using System;

namespace Acquaintance.PubSub
{
    public class ImmediatePubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;

        public ImmediatePubSubSubscription(ISubscriberReference<TPayload> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _action = action;
        }

        public Guid Id { get; set; }

        public void Publish(Envelope<TPayload> message)
        {
            _action.Invoke(message);
        }

        public bool ShouldUnsubscribe => !_action.IsAlive;
    }
}