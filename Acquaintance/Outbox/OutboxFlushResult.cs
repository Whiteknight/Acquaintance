using System;

namespace Acquaintance.Outbox
{
    public class OutboxFlushResult
    {
        public OutboxFlushResult(bool isSuccess, Exception error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }
        public Exception Error { get; }

        public static OutboxFlushResult Success()
        {
            return new OutboxFlushResult(true, null);
        }

        public static OutboxFlushResult CouldNotObtainLock()
        {
            return new OutboxFlushResult(false, null);
        }

        public static OutboxFlushResult ReceivedError(Exception e)
        {
            return new OutboxFlushResult(false, e);
        }
    }
}