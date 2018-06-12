using System.Collections.Concurrent;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    // TODO: Message TimeToLive. If the message has not been sent before the TTL is expired, drop it
    // TODO: Would like to use this for cases where a slow Subscriber provides backpressure, so we can hold the message until it's able to be delivered
    // TODO: We need a mechanism for _inner to communicate backpressure to the outbox
    public class InMemoryOutbox<TMessage> : IOutbox<TMessage>
    {
        private readonly ConcurrentQueue<OutboxItem> _messages;
        private readonly int _maxMessages;
        private readonly int _maxAttempts;
        private int _concurrentReadAttempts;

        public InMemoryOutbox(int maxMessages, int maxAttempts = int.MaxValue)
        {
            _maxMessages = maxMessages;
            _maxAttempts = maxAttempts;
            _messages = new ConcurrentQueue<OutboxItem>();
        }

        public bool AddMessage(Envelope<TMessage> message)
        {
            Assert.ArgumentNotNull(message, nameof(message));

            if (_maxMessages > 0 && _messages.Count >= _maxMessages)
                return false;
            _messages.Enqueue(new OutboxItem(message));
            return true;
        }

        public IOutboxEntry<TMessage>[] GetNextQueuedMessages(int max)
        {
            var otherReaders = Interlocked.CompareExchange(ref _concurrentReadAttempts, 1, 0);
            if (otherReaders > 0)
                return new IOutboxEntry<TMessage>[0];
            if (!_messages.TryPeek(out OutboxItem item))
                return new IOutboxEntry<TMessage>[0];
            return new IOutboxEntry<TMessage>[] { new OutboxEntry(this, item.Message) };
        }

        private void MarkForRetry(Envelope<TMessage> message)
        {
            if (!_messages.TryPeek(out OutboxItem item) || item == null || !ReferenceEquals(message, item.Message))
                return;
            
            item.Attempts++;
            if (item.Attempts >= _maxAttempts)
                _messages.TryDequeue(out item);

            ReleaseLock();
        }

        private void MarkComplete(Envelope<TMessage> message)
        {
            if (!_messages.TryPeek(out OutboxItem item) || item == null  || !ReferenceEquals(message, item.Message))
                return;
            _messages.TryDequeue(out item);

            ReleaseLock();
        }

        private void ReleaseLock()
        {
            Interlocked.MemoryBarrier();
            Interlocked.CompareExchange(ref _concurrentReadAttempts, 0, 1);
            // TODO: If it's possible for the CompareExchange to fail, we could end up in big trouble and may need to alert the user
        }

        private class OutboxItem
        {
            public OutboxItem(Envelope<TMessage> message)
            {
                Message = message;
            }

            public Envelope<TMessage> Message { get; }
            public int Attempts { get; set; }
        }

        private class OutboxEntry : IOutboxEntry<TMessage>
        {
            private readonly InMemoryOutbox<TMessage> _outbox;

            public OutboxEntry(InMemoryOutbox<TMessage> outbox, Envelope<TMessage> envelope)
            {
                _outbox = outbox;
                Envelope = envelope;
            }

            public Envelope<TMessage> Envelope { get; }


            public void MarkForRetry()
            {
                _outbox.MarkForRetry(Envelope);
            }

            public void MarkComplete()
            {
                _outbox.MarkComplete(Envelope);
            }
        }

        public int GetQueuedMessageCount()
        {
            return _messages.Count;
        }
    }
}