using System;
using Acquaintance.PubSub;
using EasyNetQ;
using EasyNetQ.FluentConfiguration;

namespace Acquaintance.RabbitMq
{
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
            if (message.LocalOnly)
                return;
            _bus.Publish(message, c => Configure(c, message.Topic));
        }

        private void Configure(IPublishConfiguration configuration, string topic)
        {
            configuration
                .WithTopic(topic)
                .WithQueueName(_queueName);
        }
    }
}
