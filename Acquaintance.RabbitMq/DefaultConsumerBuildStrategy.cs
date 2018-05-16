using EasyNetQ;

namespace Acquaintance.RabbitMq
{
    public class DefaultConsumerBuildStrategy<TPayload> : IConsumerBuildStrategy
    {
        private readonly IBus _bus;

        public DefaultConsumerBuildStrategy(IBus bus)
        {
            _bus = bus;
        }

        public ISubscriptionResult SubscribeConsumer(IMessageBus messageBus, RabbitConsumerOptions options)
        {
            string subscriberId = RabbitUtility.CreateSubscriberId();
            var rabbitToken = _bus.Subscribe<RabbitEnvelope<TPayload>>(subscriberId, remote =>
            {
                var remoteTopic = options.RemoteTopic;
                var localTopic = options.MakeLocalTopic(remoteTopic);
                var envelope = remote.ToLocalEnvelope().RedirectToTopic(localTopic);

                if (options.RemoteOnly && envelope.OriginBusId == messageBus.Id)
                    return;
                messageBus.PublishEnvelope(envelope);
            }, options.ConfigureSubscription);

            return rabbitToken;
        }

        public ISubscriptionResult ResubscribeConsumer(IMessageBus messageBus, RabbitConsumerOptions options)
        {
            return SubscribeConsumer(messageBus, options);
        }
    }
}