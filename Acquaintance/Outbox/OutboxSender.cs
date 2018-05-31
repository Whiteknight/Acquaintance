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
            var results = new OutboxSendResult();
            while (true)
            {
                var messages = _outbox.GetNextQueuedMessages(_maxBatchSize);
                if (messages == null || messages.Length == 0)
                    break;

                if (!TrySendBatch(messages, results))
                    break;
            }

            return results;
        }

        private bool TrySendBatch(IOutboxEntry<TPayload>[] messages, OutboxSendResult results)
        {
            bool hadError = false;
            int i = 0;
            for (; i < messages.Length; i++)
            {
                var message = messages[i];
                bool sentOk = TrySendMessage(message, results);
                if (!sentOk)
                {
                    hadError = true;
                    break;
                }

                message.MarkComplete();
            }

            for (; i < messages.Length; i++)
                messages[i].MarkForRetry();

            return !hadError;
        }

        private bool TrySendMessage(IOutboxEntry<TPayload> message, OutboxSendResult results)
        {
            try
            {
                _send(message.Envelope);
                results.AddSuccess(message.Envelope.Id);
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e.Message + "\n\n" + e.StackTrace);
                results.AddError(message.Envelope.Id, e);
                return false;
            }
        }
    }
}