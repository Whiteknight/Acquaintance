using System;

namespace Acquaintance.RabbitMq
{
    public interface IRabbitSenderBuilder<TPayload>
    {
        IRabbitSenderBuilder<TPayload> UseDefaultQueue();
        IRabbitSenderBuilder<TPayload> UseSpecificQueue(string queueName);
        IRabbitSenderBuilder<TPayload> WithRemoteTopic(string remoteTopic);
        IRabbitSenderBuilder<TPayload> WithMessageExpiration(int expirationMs);
        IRabbitSenderBuilder<TPayload> WithMessagePriority(byte priority);

        IRabbitSenderBuilder<TPayload> TransformTo<TRemote>(Func<Envelope<TPayload>, string, TRemote> transform)
            where TRemote : class;
    }
}