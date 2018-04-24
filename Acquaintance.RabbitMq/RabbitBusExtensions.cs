﻿using System;

namespace Acquaintance.RabbitMq
{
    public static class RabbitBusExtensions
    {
        public static void InitializeRabbitMq(this IMessageBus messageBus, string connectionString)
        {
            var module = messageBus.Modules.Get<RabbitModule>();
            if (module != null)
                throw new Exception("RabbitMQ already initialized");
            messageBus.Modules.Add(new RabbitModule(connectionString));
        }

        // TODO: Use a builder so we can get more options: Subscribe to one- or many-topics
        // subscribe to wildcard topics. Convert from AQ-style wildcards to rabbit-style wildcards
        // Be able to set values like TTL and queue expiration, QOS, durability, and other details
        // Receives messages from Rabbit and publishes them on the local bus
        public static IDisposable ForwardRabbitToLocal<TPayload>(this IMessageBus messageBus, string topic)
        {
            return GetRabbitModule(messageBus).SubscribeRemote<TPayload>(new []{ topic });
        }

        // Publishes messages from the local bus to Rabbit
        public static IDisposable ForwardLocalToRabbit<TPayload>(this IMessageBus messageBus, string[] topics)
        {
            var subscription = GetRabbitModule(messageBus).CreateForwardingSubscriber<TPayload>();
            return messageBus.Subscribe(topics, subscription);
        }

        public static IDisposable ForwardLocalToRabbit<TPayload>(this IMessageBus messageBus, string topic)
        {
            return ForwardLocalToRabbit<TPayload>(messageBus, new[] { topic ?? string.Empty });
        }

        private static RabbitModule GetRabbitModule(IMessageBus messageBus)
        {
            var module = messageBus.Modules.Get<RabbitModule>();
            if (module == null)
                throw new Exception($"RabbitMQ is not initialized. Call .{nameof(InitializeRabbitMq)}() first.");
            return module;
        }
    }
}
