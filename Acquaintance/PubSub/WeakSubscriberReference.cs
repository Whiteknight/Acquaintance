using System;

namespace Acquaintance.PubSub
{
    public class PayloadWeakSubscriberReference<TPayload> : ISubscriberReference<TPayload>
    {
        private readonly WeakReference<Action<TPayload>> _action;

        public PayloadWeakSubscriberReference(Action<TPayload> action)
        {
            _action = new WeakReference<Action<TPayload>>(action);
            IsAlive = true;
        }

        public void Invoke(Envelope<TPayload> message)
        {
            if (_action.TryGetTarget(out Action<TPayload> act))
                act(message.Payload);
            else
                IsAlive = false;
        }

        public bool IsAlive { get; private set; }
    }

    public class EnvelopeWeakSubscriberReference<TPayload> : ISubscriberReference<TPayload>
    {
        private readonly WeakReference<Action<Envelope<TPayload>>> _action;

        public EnvelopeWeakSubscriberReference(Action<Envelope<TPayload>> action)
        {
            _action = new WeakReference<Action<Envelope<TPayload>>>(action);
            IsAlive = true;
        }

        public void Invoke(Envelope<TPayload> message)
        {
            if (_action.TryGetTarget(out Action<Envelope<TPayload>> act))
                act(message);
            else
                IsAlive = false;
        }

        public bool IsAlive { get; private set; }
    }
}