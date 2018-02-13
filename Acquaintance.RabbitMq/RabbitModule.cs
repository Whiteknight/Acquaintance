using System;
using Acquaintance.PubSub;
using EasyNetQ;
using EasyNetQ.FluentConfiguration;

namespace Acquaintance.RabbitMq
{
    public class RabbitModule : IMessageBusModule
    {
        private readonly IBus _bus;
        private IMessageBus _messageBus;

        public RabbitModule(string connectionString)
        {
            _bus = RabbitHutch.CreateBus(connectionString);
        }

        public void Attach(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Unattach()
        {
            _messageBus = null;
        }

        public IDisposable SubscribeRemote<TPayload>(string topic)
        {
            string id = CreateSubscriberId();
            var queueName = MakeQueueName<TPayload>();
            return _bus.Subscribe<Envelope<TPayload>>(id, envelope => {
                if (envelope.OriginBusId == _messageBus.Id)
                    _messageBus.PublishEnvelope(envelope);
            }, c => Configure(c, topic, queueName));
        }

        public ISubscription<TPayload> CreateForwardingSubscriber<TPayload>()
        {
            var queueName = MakeQueueName<TPayload>();
            return new ForwardToRabbitSubscription<TPayload>(_bus, queueName);
        }

        public static string MakeQueueName<TPayload>()
        {
            var t = typeof(TPayload);
            return $"AQ:{t.Namespace}.{t.Name}";
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        ~RabbitModule()
        {
            Dispose(false);
        }

        private static void Configure(ISubscriptionConfiguration configuration, string topic, string queueName)
        {
            configuration
                .WithTopic(topic ?? string.Empty)
                .WithQueueName(queueName);
        }

        private static string CreateSubscriberId()
        {
            var id = Guid.NewGuid().ToString();
            return id;
        }

        private void Dispose(bool disposing)
        {
            _bus.Dispose();
        }
    }
}
