using System;
using System.Collections.Generic;
using System.Linq;

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
        public OutboxSendResult(bool success, IReadOnlyList<MessageSendResult> results)
        {
            Results = results ?? new List<MessageSendResult>();
            Success = success;
        }

        public bool Success { get; }

        public IReadOnlyList<MessageSendResult> Results { get; }
    }

    public class OutboxSendResultBuilder
    {
        private readonly List<MessageSendResult> _results;

        public OutboxSendResultBuilder()
        {
            _results = new List<MessageSendResult>();
        }

        public OutboxSendResult Build()
        {
            return new OutboxSendResult(_results.All(r => r.Result == OutboxSendResultType.SendSuccess), _results);
        }

        public void AddSuccess(long messageId)
        {
            _results.Add(new MessageSendResult(messageId, OutboxSendResultType.SendSuccess));
        }

        public void AddError(long messageId, Exception e)
        {
            _results.Add(new MessageSendResult(messageId, OutboxSendResultType.SendFailed, e));
        }
    }
}