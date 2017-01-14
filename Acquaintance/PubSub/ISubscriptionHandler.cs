using System;

namespace Acquaintance.PubSub
{
    public interface ISubscriptionHandler<in TPayload>
    {
        void Handle(TPayload payload);
    }

    public class SubscriptionHandlerActionReference<TPayload> : ISubscriberReference<TPayload>
    {
        private readonly ISubscriptionHandler<TPayload> _handler;

        public SubscriptionHandlerActionReference(ISubscriptionHandler<TPayload> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            _handler = handler;
        }

        public void Invoke(TPayload payload)
        {
            _handler.Handle(payload);
        }

        public bool IsAlive => true;
    }
}
