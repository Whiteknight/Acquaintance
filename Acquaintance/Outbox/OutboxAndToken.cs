using System;

namespace Acquaintance.Outbox
{
    public sealed class OutboxAndToken<TMessage> : IDisposable
    {
        public OutboxAndToken(IOutbox<TMessage> outbox, IDisposable token)
        {
            Outbox = outbox;
            Token = token;
        }

        public IDisposable Token { get; }
        public IOutbox<TMessage> Outbox { get; }

        public void Dispose()
        {
            Token?.Dispose();
        }
    }
}