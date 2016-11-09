namespace Acquaintance.PubSub
{
    public class ImmediatePubSubSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscriberReference<TPayload> _action;

        public ImmediatePubSubSubscription(ISubscriberReference<TPayload> action)
        {
            _action = action;
        }

        public void Publish(TPayload payload)
        {
            _action.Invoke(payload);
        }

        public bool ShouldUnsubscribe => !_action.IsAlive;
    }
}