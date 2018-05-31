using System;

namespace Acquaintance.Outbox
{
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
            (Outbox as IDisposable)?.Dispose();
            (Sender as IDisposable)?.Dispose();
            _monitorToken?.Dispose();
        }
    }
}