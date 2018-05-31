using System;
using Acquaintance.Outbox;
using Acquaintance.PubSub;
using Acquaintance.Utility;
using EasyNetQ;
using EasyNetQ.FluentConfiguration;

namespace Acquaintance.RabbitMq
{
    public sealed class ForwardToRabbitSubscription<TPayload> : ISubscription<TPayload>
    {
        private readonly IBus _rabbitBus;
        private readonly string _queueName;
        private readonly SendingOutbox<TPayload> _outbox;
        
        public ForwardToRabbitSubscription(IBusBase messageBus, IBus rabbitBus, string queueName, IOutbox<TPayload> outbox)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(rabbitBus, nameof(rabbitBus));
            Assert.ArgumentNotNull(outbox, nameof(outbox));

            _rabbitBus = rabbitBus;
            _queueName = queueName;
            var sender = new OutboxSender<TPayload>(messageBus.Logger, outbox, PublishInternal);
            var outboxToken = messageBus.AddOutboxToBeMonitored(sender);
            _outbox = new SendingOutbox<TPayload>(outbox, sender, outboxToken);
        }

        public void Publish(Envelope<TPayload> message)
        {
            _outbox.SendMessage(message);
        }

        public bool ShouldUnsubscribe => false;
        public Guid Id { get; set; }

        private void PublishInternal(Envelope<TPayload> message)
        {
            foreach (var topic in message.Topics)
                _rabbitBus.Publish(message, c => Configure(c, topic));
        }

        private void Configure(IPublishConfiguration configuration, string topic)
        {
            configuration
                .WithTopic(topic)
                .WithQueueName(_queueName);
        }

        public void Dispose()
        {
            _outbox.Dispose();
        }
    }
}
