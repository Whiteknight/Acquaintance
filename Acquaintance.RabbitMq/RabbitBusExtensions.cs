using System;
using Acquaintance.PubSub;

namespace Acquaintance.RabbitMq
{
    public static class RabbitBusExtensions
    {
        private static readonly Guid _id = Guid.NewGuid();

        public static void InitializeRabbitMq(this IMessageBus messageBus, string connectionString)
        {
            var module = messageBus.Modules.Get<RabbitModule>(_id);
            if (module != null)
                throw new Exception("RabbitMQ already initialized");
            messageBus.Modules.Add(_id, new RabbitModule(connectionString));
        }

        // Receives messages from Rabbit and publishes them on the local bus
        public static IDisposable ForwardRabbitToLocal<TPayload>(this IMessageBus messageBus, string topic)
        {
            return GetRabbitModule(messageBus).SubscribeRemote<TPayload>(topic);
        }

        // Publishes messages from the local bus to Rabbit
        public static ISubscription<TPayload> ForwardLocalToRabbit<TPayload>(this IMessageBus messageBus)
        {
            return GetRabbitModule(messageBus).CreateForwardingSubscriber<TPayload>();
        }

        private static RabbitModule GetRabbitModule(IMessageBus messageBus)
        {
            var module = messageBus.Modules.Get<RabbitModule>(_id);
            if (module == null)
                throw new Exception($"RabbitMQ is not initialized. Call .{nameof(InitializeRabbitMq)}() first.");
            return module;
        }
    }
}
