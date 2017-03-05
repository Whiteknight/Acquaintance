using System;

namespace Acquaintance.PubSub
{
    public class PayloadStrongSubscriberReference<TPayload> : ISubscriberReference<TPayload>
    {
        private readonly Action<TPayload> _action;

        public PayloadStrongSubscriberReference(Action<TPayload> action)
        {
            _action = action;
        }

        public void Invoke(Envelope<TPayload> message)
        {
            _action(message.Payload);
        }

        public bool IsAlive => true;
    }

    public class EnvelopeStrongSubscriberReference<TPayload> : ISubscriberReference<TPayload>
    {
        private readonly Action<Envelope<TPayload>> _action;

        public EnvelopeStrongSubscriberReference(Action<Envelope<TPayload>> action)
        {
            _action = action;
        }

        public void Invoke(Envelope<TPayload> message)
        {
            _action(message);
        }

        public bool IsAlive => true;
    }
}