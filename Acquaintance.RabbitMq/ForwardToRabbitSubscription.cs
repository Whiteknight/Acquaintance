using System;
using Acquaintance.PubSub;
using EasyNetQ;
using EasyNetQ.FluentConfiguration;

namespace Acquaintance.RabbitMq
{
    // TODO: Some kind of Outbox implementation for reliable publishing
    public class ForwardToRabbitSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly IBus _bus;
        private readonly string _queueName;

        public ForwardToRabbitSubscription(IBus bus, string queueName)
        {
            _bus = bus;
            _queueName = queueName;
        }

        public bool ShouldUnsubscribe => false;
        public Guid Id { get; set; }

        public void Publish(Envelope<TPayload> message)
        {
            foreach (var topic in message.Topics)
                _bus.Publish(message, c => Configure(c, topic));
        }

        private void Configure(IPublishConfiguration configuration, string topic)
        {
            configuration
                .WithTopic(topic)
                .WithQueueName(_queueName);
        }
    }
}
