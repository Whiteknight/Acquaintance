using System;

namespace Acquaintance.PubSub
{
    public class WeakSubscriberReference<TPayload> : ISubscriberReference<TPayload>
    {
        private readonly WeakReference<Action<TPayload>> _action;

        public WeakSubscriberReference(Action<TPayload> action)
        {
            _action = new WeakReference<Action<TPayload>>(action);
            IsAlive = true;
        }

        public void Invoke(TPayload payload)
        {
            Action<TPayload> act;
            if (_action.TryGetTarget(out act))
                act(payload);
            else
                IsAlive = false;
        }

        public bool IsAlive { get; private set; }
    }
}