using System;
using Acquaintance.Utility;

namespace Acquaintance.Outbox
{
    // Facade over IOutbox<T> and IOutboxSender to simplify storage and common operations
    public sealed class SendingOutbox<TPayload> : IDisposable
    {
        private readonly IDisposable _monitorToken;

        public SendingOutbox(IOutbox<TPayload> outbox, IOutboxSender sender, IDisposable monitorToken = null)
        {
            _monitorToken = monitorToken;
            Outbox = outbox;
            Sender = sender;
        }

        public IOutbox<TPayload> Outbox { get; }
        public IOutboxSender Sender { get; }

        public bool SendMessage(Envelope<TPayload> message)
        {
            return Outbox.AddMessage(message) && Sender.TrySend().Success;
        }

        public void TrySendAll()
        {
            Sender.TrySend();
        }

        public void Dispose()
        {
            ObjectManagement.TryDispose(Outbox);
            ObjectManagement.TryDispose(Sender);
            _monitorToken?.Dispose();
        }
    }
}