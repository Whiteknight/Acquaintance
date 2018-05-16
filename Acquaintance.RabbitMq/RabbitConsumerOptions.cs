using System;
using EasyNetQ.FluentConfiguration;

namespace Acquaintance.RabbitMq
{
    // TODO: This class needs to be immutable
    public class RabbitConsumerOptions
    {
        public string ExchangeName { get; set; }
        public string ExchangeType { get; set; }
        // TODO: Coalesce this to "#"
        public string RemoteTopic { get; set; }
        public Func<string, string> MakeLocalTopic { get; set; }
        public string QueueName { get; set; }
        public bool RawMessages { get; set; }
        public int? QueueExpirationMs { get; set; }
        public bool RemoteOnly { get; set; }

        public void ConfigureSubscription(ISubscriptionConfiguration configuration)
        {
            // TODO: configuration.WithDurable
            // TODO: configuration.WithPriority
            // TODO: configuration.WithPrefetchCount
            configuration
                .WithTopic(RemoteTopic ?? string.Empty)
                .WithQueueName(QueueName);

            if (QueueExpirationMs.HasValue && QueueExpirationMs > 0)
                configuration.WithExpires(QueueExpirationMs.Value);

            // TODO: configuration.AsExclusive
        }
    }
}