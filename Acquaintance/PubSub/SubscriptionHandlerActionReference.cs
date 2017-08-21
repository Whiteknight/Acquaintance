using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class SubscriptionHandlerActionReference<TPayload> : ISubscriberReference<TPayload>
    {
        private readonly ISubscriptionHandler<TPayload> _handler;

        public SubscriptionHandlerActionReference(ISubscriptionHandler<TPayload> handler)
        {
            Assert.ArgumentNotNull(handler, nameof(handler));
            _handler = handler;
        }

        public void Invoke(Envelope<TPayload> message)
        {
            _handler.Handle(message);
        }

        public bool IsAlive => true;
    }
}