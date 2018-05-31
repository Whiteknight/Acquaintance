using System;
using System.Collections.Generic;

namespace Acquaintance.Outbox
{
    public interface IOutboxSendResult
    {
        IReadOnlyList<MessageSendResult> Results { get; }
        bool Success { get; }
    }

    public class MessageSendResult
    {
        public MessageSendResult(long messageId, OutboxSendResultType result)
            : this(messageId, result, null)
        {
        }

        public MessageSendResult(long messageId, OutboxSendResultType result, Exception exception)
        {
            MessageId = messageId;
            Result = result;
            Exception = exception;
        }

        public long MessageId { get; }
        public OutboxSendResultType Result { get; }
        public Exception Exception { get; }
    }

    public class OutboxSendResult : IOutboxSendResult
    {
        private readonly List<MessageSendResult> _results;
        public OutboxSendResult()
        {
            _results = new List<MessageSendResult>();
            Success = true;
        }

        public bool Success { get; private set; }

        public IReadOnlyList<MessageSendResult> Results => _results;

        public void AddSuccess(long messageId)
        {
            _results.Add(new MessageSendResult(messageId, OutboxSendResultType.SendSuccess));
        }

        public void AddError(long messageId, Exception e)
        {
            _results.Add(new MessageSendResult(messageId, OutboxSendResultType.SendFailed, e));
            Success = false;
        }
    }
}