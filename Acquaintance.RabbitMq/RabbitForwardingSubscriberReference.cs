using System;
using Acquaintance.PubSub;
using EasyNetQ;
using EasyNetQ.FluentConfiguration;

namespace Acquaintance.RabbitMq
{
    public class RabbitForwardingSubscriberReference<TPayload, TRemote> : ISubscriberReference<TPayload>
        where TRemote : class
    {
        private readonly IBus _bus;
        private readonly RabbitSenderOptions _options;
        private readonly Func<Envelope<TPayload>, string, TRemote> _transform;

        public RabbitForwardingSubscriberReference(IBus bus, RabbitSenderOptions options, Func<Envelope<TPayload>, string, TRemote> transform)
        {
            _bus = bus;
            _options = options;
            _transform = transform;
        }

        public bool IsAlive => true;

        public void Invoke(Envelope<TPayload> message)
        {
            var topics = string.IsNullOrEmpty(_options.RemoteTopic) ? message.Topics : new[] { _options.RemoteTopic };
            if (topics == null || topics.Length == 0)
                topics = new[] { "" };
            
            // TODO: Handle exceptions when the publish fails or timesout, and communicate that failure back to the module
            foreach (var topic in topics)
            {
                var remote = _transform(message, topic);
                _bus.Publish(remote, c => Configure(c, topic));
            }
        }

        public void Dispose()
        {
        }

        private void Configure(IPublishConfiguration configuration, string topic)
        {
            configuration
                .WithTopic(topic)
                .WithExpires(_options.MessageExpirationMs)
                .WithPriority(_options.MessagePriority)
                ;
            if (!string.IsNullOrEmpty(_options.QueueName))
                configuration.WithQueueName(_options.QueueName);
        }
    }
}
