using System;
using EasyNetQ;

namespace Acquaintance.RabbitMq
{
    public class TransformingConsumerBuildStrategy<TPayload, TRemote> : IConsumerBuildStrategy
        where TRemote : class
    {
        private readonly IBus _bus;
        private readonly Func<TRemote, string, Envelope<TPayload>> _transform;
        private readonly Func<TRemote, string> _getTopic;

        public TransformingConsumerBuildStrategy(IBus bus, Func<TRemote, string, Envelope<TPayload>> transform, Func<TRemote, string> getTopic)
        {
            _bus = bus;
            _transform = transform;
            _getTopic = getTopic;
        }

        public ISubscriptionResult SubscribeConsumer(IMessageBus messageBus, RabbitConsumerOptions options)
        {
            string subscriberId = RabbitUtility.CreateSubscriberId();
            var rabbitToken = _bus.Subscribe<TRemote>(subscriberId, remote =>
            {
                var remoteTopic = _getTopic?.Invoke(remote) ?? options.RemoteTopic;
                var localTopic = options.MakeLocalTopic(remoteTopic);
                var envelope = _transform(remote, remoteTopic).RedirectToTopic(localTopic);

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