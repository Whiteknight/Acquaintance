using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    public sealed class OutboxAndToken<TMessage> : IDisposable
    {
        public OutboxAndToken(IOutbox<TMessage> outbox, IDisposable token)
        {
            Assert.ArgumentNotNull(outbox, nameof(outbox));
            Assert.ArgumentNotNull(token, nameof(token));

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