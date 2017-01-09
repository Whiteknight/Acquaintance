using Acquaintance.Threading;

namespace Acquaintance.PubSub
{
    public class PublishEventThreadAction<TPayload> : IThreadAction
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly TPayload _payload;

        public PublishEventThreadAction(ISubscriberReference<TPayload> action, TPayload payload)
        {
            _action = action;
            _payload = payload;
        }

        public void Execute()
        {
            _action.Invoke(_payload);
        }
    }
}