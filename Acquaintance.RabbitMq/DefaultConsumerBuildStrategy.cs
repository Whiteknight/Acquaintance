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
                var envelope = messageBus.EnvelopeFactory.CreateFromRemote(remote.OriginBusId, new[] { localTopic }, remote.Payload, remote.Metadata);

                // TODO: We want to keep a running receipt of busId:messageId for all hops
                // Probably need to do appending in the sender, not the receiver
                // TODO: Need a parser for this data, so we can extract a strongly-typed itinerary
                envelope.AppendMetadata("OriginEnvelopeId", remote.Id.ToString);

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