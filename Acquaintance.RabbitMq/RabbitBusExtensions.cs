using System;
using Acquaintance.PubSub;
using Acquaintance.Utility;

namespace Acquaintance.RabbitMq
{
    public static class RabbitBusExtensions
    {
        public static IDisposable InitializeRabbitMq(this IMessageBus messageBus, string connectionString)
        {
            var module = messageBus.Modules.Get<RabbitModule>();
            if (module != null)
                throw new Exception("RabbitMQ already initialized");
            return messageBus.Modules.Add(new RabbitModule(messageBus, connectionString));
        }

        public static IDisposable InitializeRabbitMq(this IMessageBus messageBus, string host, string username, string password)
        {
            return InitializeRabbitMq(messageBus, $"host={host};username={username};password={password}");
        }

        public static IDisposable PullRabbitMqToLocal<TPayload>(this IBusBase messageBus, Action<IRabbitConsumerBuilderRemoteTopic<TPayload>> setup)
            where TPayload : class
        {
            var module = GetRabbitModuleOrThrow(messageBus);
            var builder = module.CreateSubscriberBuilder<TPayload>();
            setup(builder);
            var receiver = builder.Build();
            return module.ManageConsumer(receiver);
        }

        public static ISubscriberReference<TPayload> CreateRabbitMqForwardingSubscription<TPayload>(this IBusBase messageBus, Action<IRabbitSenderBuilder<TPayload>> setup)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(setup, nameof(setup));

            var module = GetRabbitModuleOrThrow(messageBus);
            var builder = module.CreatePublisherBuilder<TPayload>();
            setup(builder);
            return builder.Build();
        }

        private static RabbitModule GetRabbitModuleOrThrow(IBusBase messageBus)
        {
            var module = messageBus.Modules.Get<RabbitModule>();
            if (module == null)
                throw new Exception($"RabbitMQ is not initialized. Call .{nameof(InitializeRabbitMq)}() first.");
            return module;
        }
    }
}
