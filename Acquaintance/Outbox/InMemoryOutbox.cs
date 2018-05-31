using System.Collections.Concurrent;
using System.Threading;

namespace Acquaintance.Outbox
{
    // TODO: Message TimeToLive. If the message has not been sent before the TTL is expired, drop it
    // TODO: MaxRetries. If we exceed the maximum number of retries on a message, drop it
    // TODO: Would like to use this for cases where a slow Subscriber provides backpressure, so we can hold the message until it's able to be delivered
    // TODO: We need a mechanism for _inner to communicate backpressure to the outbox
    public class InMemoryOutbox<TMessage> : IOutbox<TMessage>
    {
        private readonly ConcurrentQueue<Envelope<TMessage>> _messages;
        private readonly int _maxMessages;
        private int _concurrentReadAttempts;

        public InMemoryOutbox(int maxMessages)
        {
            _maxMessages = maxMessages;
            _messages = new ConcurrentQueue<Envelope<TMessage>>();
        }

        public bool AddMessage(Envelope<TMessage> message)
        {
            if (_maxMessages > 0 && _messages.Count >= _maxMessages)
                return false;
            _messages.Enqueue(message);
            return true;
        }

        public IOutboxEntry<TMessage>[] GetNextQueuedMessages(int max)
        {
            var otherReaders = Interlocked.CompareExchange(ref _concurrentReadAttempts, 1, 0);
            if (otherReaders > 0)
                return new IOutboxEntry<TMessage>[0];
            if (!_messages.TryPeek(out Envelope<TMessage> message))
                return new IOutboxEntry<TMessage>[0];
            return new IOutboxEntry<TMessage>[] { new InMemoryOutboxEntry(this, message) };
        }

        private void MarkForRetry(Envelope<TMessage> message)
        {
            _concurrentReadAttempts = 0;
        }

        private void MarkComplete(Envelope<TMessage> message)
        {
            _concurrentReadAttempts = 0;
            _messages.TryPeek(out Envelope<TMessage> queueMessage);
            if (!ReferenceEquals(message, queueMessage))
                return;
            _messages.TryDequeue(out queueMessage);
        }

        public class InMemoryOutboxEntry : IOutboxEntry<TMessage>
        {
            private readonly InMemoryOutbox<TMessage> _outbox;

            public InMemoryOutboxEntry(InMemoryOutbox<TMessage> outbox, Envelope<TMessage> envelope)
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