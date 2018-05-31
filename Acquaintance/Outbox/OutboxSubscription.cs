using System;
using Acquaintance.PubSub;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public sealed class OutboxSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscription<TPayload> _inner;
        private readonly SendingOutbox<TPayload> _outbox;

        public OutboxSubscription(IBusBase messageBus, ISubscription<TPayload> inner, IOutbox<TPayload> outbox)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(inner, nameof(inner));
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            _inner = inner;
            var sender = new OutboxSender<TPayload>(messageBus.Logger, outbox, PublishInternal);
            var outboxToken = messageBus.TryAddOutboxToBeMonitored(sender);
            _outbox = new SendingOutbox<TPayload>(outbox, sender, outboxToken);
        }

        public static ISubscription<TPayload> WrapSubscription(IBusBase messageBus, ISubscription<TPayload> inner, IOutbox<TPayload> outbox)
        {
            if (outbox == null)
                return inner;
            return new OutboxSubscription<TPayload>(messageBus, inner, outbox);
        }

        public void Publish(Envelope<TPayload> message)
        {
            _outbox.SendMessage(message);
        }

        private void PublishInternal(Envelope<TPayload> message)
        {
            _inner.Publish(message);
        }

        public bool ShouldUnsubscribe => _inner.ShouldUnsubscribe;

        public Guid Id
        {
            get => _inner.Id;
            set => _inner.Id = value;
        }

        public void Dispose()
        {
            _outbox?.Dispose();
            _inner?.Dispose();
        }
    }
}