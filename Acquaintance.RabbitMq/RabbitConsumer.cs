using System;
using EasyNetQ;

namespace Acquaintance.RabbitMq
{
    public sealed class RabbitConsumer : IDisposable
    {
        public RabbitConsumer(IMessageBus messageBus, IConsumerBuildStrategy buildStrategy, RabbitConsumerOptions queue, ISubscriptionResult rabbitToken)
        {
            MessageBus = messageBus;
            BuildStrategy = buildStrategy;
            Queue = queue;
            RabbitToken = rabbitToken;
        }

        public IMessageBus MessageBus { get; }
        public IConsumerBuildStrategy BuildStrategy { get; }
        public RabbitConsumerOptions Queue { get; }
        public ISubscriptionResult RabbitToken { get; private set; }

        public void Reconnect()
        {
            RabbitToken.Dispose();
            var newToken = BuildStrategy.ResubscribeConsumer(MessageBus, Queue);
            RabbitToken = newToken;
        }

        public void Dispose()
        {
            RabbitToken?.Dispose();
        }
    }
}