using System;
using Acquaintance.PubSub;
using EasyNetQ;
using RabbitMQ.Client.MessagePatterns;

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

        public void Dispose()
        {
            _bus.Dispose();
        }

        public IDisposable SubscribeRemote<TPayload>(string topic)
        {
            string id = CreateSubscriberId();
            return _bus.Subscribe<Envelope<TPayload>>(id, envelope => {
                _messageBus.PublishEnvelope(envelope.ForLocalDelivery());
            });
        }

        public ISubscription<TPayload> CreateForwardingSubscriber<TPayload>()
        {
            return new RabbitSubscription<TPayload>(_bus);
        }

        private static string CreateSubscriberId()
        {
            var id = Guid.NewGuid().ToString();
            return id;
        }

        ~RabbitModule()
        {
            Dispose();
        }
    }
}
