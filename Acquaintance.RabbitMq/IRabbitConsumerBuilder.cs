using System;

namespace Acquaintance.RabbitMq
{
    public interface IRabbitConsumerBuilderRemoteTopic<TPayload>
    {
        IRabbitConsumerBuilderRemoteMessage<TPayload> WithRemoteTopic(string remoteTopic);
        IRabbitConsumerBuilderRemoteMessage<TPayload> ForAllRemoteTopics();
    }

    public interface IRabbitConsumerBuilderRemoteMessage<TPayload>
    {
        IRabbitConsumerBuilderLocalTopic<TPayload> ReceiveDefaultFormat();
        IRabbitConsumerBuilderLocalTopic<TPayload> ReceiveRawMessage();

        IRabbitConsumerBuilderLocalTopic<TPayload> TransformFrom<TRemote>(Func<TRemote, string, Envelope<TPayload>> transform, Func<TRemote, string> getTopic = null)
            where TRemote : class;
    }

    public interface IRabbitConsumerBuilderLocalTopic<TPayload>
    {
        IRabbitConsumerBuilderQueue<TPayload> ForwardToLocalTopic(string localTopic);
        IRabbitConsumerBuilderQueue<TPayload> ForwardToLocalTopic(Func<string, string> transformTopic);
    }

    public interface IRabbitConsumerBuilderQueue<TPayload>
    {
        IRabbitConsumerBuilderDetails<TPayload> UseUniqueQueue();
        IRabbitConsumerBuilderDetails<TPayload> UseSharedQueueName();
        IRabbitConsumerBuilderDetails<TPayload> UseQueueName(string queueName);
    }

    public interface IRabbitConsumerBuilderDetails<TPayload>
    {
        // TODO: Exclusive
        // TODO: Durable
        // TODO: Prefetch count
        IRabbitConsumerBuilderDetails<TPayload> AutoExpireQueue();
        IRabbitConsumerBuilderDetails<TPayload> ExpireQueue(int expirationMs);
        IRabbitConsumerBuilderDetails<TPayload> ReceiveRemoteMessagesOnly();
    }
}