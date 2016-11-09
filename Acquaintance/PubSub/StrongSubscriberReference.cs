using System;

namespace Acquaintance.PubSub
{
    public class StrongSubscriberReference<TPayload> : ISubscriberReference<TPayload>
    {
        private readonly Action<TPayload> _action;

        public StrongSubscriberReference(Action<TPayload> action)
        {
            _action = action;
        }

        public void Invoke(TPayload payload)
        {
            _action(payload);
        }

        public bool IsAlive => true;
    }
}