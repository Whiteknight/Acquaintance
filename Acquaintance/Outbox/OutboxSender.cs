using System;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public class OutboxSender<TPayload> : IOutboxSender
    {
        private readonly ILogger _logger;
        private readonly IOutbox<TPayload> _outbox;
        private readonly Action<Envelope<TPayload>> _send;
        private readonly int _maxBatchSize;

        public OutboxSender(ILogger logger, IOutbox<TPayload> outbox, Action<Envelope<TPayload>> send, int maxBatchSize = 100)
        {
            Assert.ArgumentNotNull(outbox, nameof(outbox));
            Assert.ArgumentNotNull(send, nameof(send));

            _logger = logger;
            _outbox = outbox;
            _send = send;
            _maxBatchSize = maxBatchSize;
        }

        public IOutboxSendResult TrySend()
        {
            var results = new OutboxSendResultBuilder();
            while (true)
            {
                var messages = _outbox.GetNextQueuedMessages(_maxBatchSize);
                if (messages == null || messages.Length == 0)
                    break;

                var numSent = TrySendBatch(messages, results);
                if (numSent < messages.Length)
                    break;
            }

            return results.Build();
        }

        private int TrySendBatch(IOutboxEntry<TPayload>[] messages, OutboxSendResultBuilder results)
        {
            // TODO: Need to cleanup this logic
            int messagesSent = 0;
            int i = 0;
            for (; i < messages.Length; i++)
            {
                var message = messages[i];
                try
                {
                    bool sentOk = TrySendMessage(message, results);
                    if (!sentOk)
                    {
                        message.MarkForRetry();
                        break;
                    }

                    messagesSent++;
                    message.MarkComplete();
                }
                catch(Exception e)
                {
                    message.MarkForRetry();
                    _logger.Error(e, "Error during message send or bookkeeping. Message will be marked for retry");
                }
            }

            for (; i < messages.Length; i++)
            {
                results.AddNotAttempted(messages[i].Envelope.Id);
                messages[i].MarkForRetry();
            }

            return messagesSent;
        }

        private bool TrySendMessage(IOutboxEntry<TPayload> message, OutboxSendResultBuilder results)
        {
            try
            {
                _send(message.Envelope);
                results.AddSuccess(message.Envelope.Id);
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error during message send. Message will be marked for retry.");
                results.AddError(message.Envelope.Id, e);
                return false;
            }
        }
    }
}