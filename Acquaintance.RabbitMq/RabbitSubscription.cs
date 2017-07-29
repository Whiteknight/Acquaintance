using System;
using Acquaintance.PubSub;
using EasyNetQ;

namespace Acquaintance.RabbitMq
{
    public class RabbitSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly IBus _bus;

        public RabbitSubscription(IBus bus)
        {
            _bus = bus;
        }

        public bool ShouldUnsubscribe => false;
        public Guid Id { get; set; }

        public void Publish(Envelope<TPayload> message)
        {
            if (message.LocalOnly)
                return;
            _bus.Publish(message);
        }
    }
}
