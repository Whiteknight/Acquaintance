using EasyNetQ;

namespace Acquaintance.RabbitMq
{
    public interface IConsumerBuildStrategy
    {
        ISubscriptionResult SubscribeConsumer(IMessageBus messageBus, RabbitConsumerOptions options);
        ISubscriptionResult ResubscribeConsumer(IMessageBus messageBus, RabbitConsumerOptions options);
    }
}