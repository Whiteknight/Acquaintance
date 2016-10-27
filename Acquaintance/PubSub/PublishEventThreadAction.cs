using Acquaintance.Threading;
using System;

namespace Acquaintance.PubSub
{
    public class PublishEventThreadAction<TPayload> : IThreadAction
    {
        private readonly Action<TPayload> _act;
        private readonly TPayload _payload;

        public PublishEventThreadAction(Action<TPayload> act, TPayload payload)
        {
            _act = act;
            _payload = payload;
        }

        public void Execute(IMessageHandlerThreadContext threadContext)
        {
            _act(_payload);
        }
    }
}