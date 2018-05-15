using System;
using Acquaintance.Outbox;
using Acquaintance.PubSub;
using EasyNetQ;
using EasyNetQ.FluentConfiguration;

namespace Acquaintance.RabbitMq
{
    public class ForwardToRabbitSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly IBus _bus;
        private readonly string _queueName;
        private readonly IOutbox<TPayload> _outbox;
        private readonly IDisposable _outboxToken;

        public ForwardToRabbitSubscription(IBus bus, string queueName, IOutboxFactory outboxFactory)
        {
            _bus = bus;
            _queueName = queueName;
            var outbox = outboxFactory.Create<TPayload>(PublishInternal);
            _outbox = outbox.Outbox;
            _outboxToken = outbox.Token;
        }

        public void Publish(Envelope<TPayload> message)
        {
            _outbox.AddMessage(message);
            _outbox.TryFlush();
        }

        public bool ShouldUnsubscribe => false;
        public Guid Id { get; set; }

        private void PublishInternal(Envelope<TPayload> message)
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

        public void Dispose()
        {
            _outboxToken.Dispose();
        }
    }
}
