using System;
using Acquaintance.PubSub;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public sealed class OutboxSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly ISubscription<TPayload> _inner;
        private readonly IOutbox<TPayload> _outbox;
        private readonly IDisposable _outboxToken;

        public OutboxSubscription(ISubscription<TPayload> inner, IOutboxFactory outboxFactory)
        {
            Assert.ArgumentNotNull(inner, nameof(inner));
            Assert.ArgumentNotNull(outboxFactory, nameof(outboxFactory));

            _inner = inner;
            var outbox = outboxFactory.Create<TPayload>(PublishInternal);
            _outbox = outbox.Outbox;
            _outboxToken = outbox.Token;
        }

        public OutboxSubscription(IOutbox<TPayload> outbox)
        {
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            _outbox = outbox;
        }

        public static ISubscription<TPayload> WrapSubscription(ISubscription<TPayload> inner, IOutboxFactory outboxFactory)
        {
            if (outboxFactory == null)
                return inner;
            return new OutboxSubscription<TPayload>(inner, outboxFactory);
        }

        public void Publish(Envelope<TPayload> message)
        {
            _outbox.AddMessage(message);
            _outbox.TryFlush();
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
            _outboxToken?.Dispose();
            (_outbox as IDisposable)?.Dispose();
            _inner?.Dispose();
        }
    }

    public class OutboxSubscriberReference<TPayload> : ISubscriberReference<TPayload>
    {
        private readonly IOutbox<TPayload> _outbox;

        public OutboxSubscriberReference(IOutbox<TPayload> outbox)
        {
            _outbox = outbox;
        }

        public void Invoke(Envelope<TPayload> message)
        {
            _outbox.AddMessage(message);
            _outbox.TryFlush();
        }

        public bool IsAlive => true;
    }
}