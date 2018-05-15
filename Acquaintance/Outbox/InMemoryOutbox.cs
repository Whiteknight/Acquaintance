using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Acquaintance.Outbox
{
    // TODO: Message TimeToLive. If the message has not been sent before the TTL is expired, drop it
    // TODO: MaxRetries. If we exceed the maximum number of retries on a message, drop it
    // TODO: Would like to use this for cases where a slow Subscriber provides backpressure, so we can hold the message until it's able to be delivered
    // TODO: We need a mechanism for _inner to communicate backpressure to the outbox
    internal class InMemoryOutbox<TMessage> : IOutbox<TMessage>
    {
        private readonly Action<Envelope<TMessage>> _outputPort;
        private readonly ConcurrentQueue<Envelope<TMessage>> _messages;
        private readonly int _maxMessages;
        private int _readers;

        public InMemoryOutbox(Action<Envelope<TMessage>> outputPort, int maxMessages)
        {
            _outputPort = outputPort;
            _maxMessages = maxMessages;
            _messages = new ConcurrentQueue<Envelope<TMessage>>();
            _readers = 0;
        }

        public bool AddMessage(Envelope<TMessage> message)
        {
            if (_maxMessages > 0 && _messages.Count >= _maxMessages)
                return false;
            _messages.Enqueue(message);
            return true;
        }

        public OutboxFlushResult TryFlush()
        {
            var readers = Interlocked.CompareExchange(ref _readers, 1, 0);
            if (readers != 0)
                return OutboxFlushResult.CouldNotObtainLock();

            try
            {
                return TryFlushQueueInternal();
            }
            finally
            {
                _readers = 0;
            }
        }

        private OutboxFlushResult TryFlushQueueInternal()
        {
            while (true)
            {
                if (!_messages.TryPeek(out Envelope<TMessage> message))
                    break;
                try
                {
                    _outputPort(message);
                }
                catch (Exception e)
                {
                    return OutboxFlushResult.ReceivedError(e);
                }

                _messages.TryDequeue(out message);
            }

            return OutboxFlushResult.Success();
        }

        public int GetQueuedMessageCount()
        {
            return _messages.Count;
        }
    }
}