using Acquaintance.Threading;

namespace Acquaintance.PubSub
{
    public class PublishEventThreadAction<TPayload> : IThreadAction
    {
        private readonly ISubscriberReference<TPayload> _action;
        private readonly Envelope<TPayload> _message;

        public PublishEventThreadAction(ISubscriberReference<TPayload> action, Envelope<TPayload> message)
        {
            _action = action;
            _message = message;
        }

        public void Execute()
        {
            _action.Invoke(_message);
        }
    }
}