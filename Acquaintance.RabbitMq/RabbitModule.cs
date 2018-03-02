using System;
using System.Collections.Generic;
using Acquaintance.PubSub;
using EasyNetQ;
using EasyNetQ.FluentConfiguration;

namespace Acquaintance.RabbitMq
{
    public class RabbitModule : IMessageBusModule, IDisposable
    {
        private readonly IBus _bus;
        private readonly HashSet<IDisposable> _tokens;
        private IMessageBus _messageBus;
        private bool _disposing;

        public RabbitModule(string connectionString)
        {
            _bus = RabbitHutch.CreateBus(connectionString);
            _tokens = new HashSet<IDisposable>();
            _disposing = false;
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
            var innerToken = _bus.Subscribe<Envelope<TPayload>>(id, envelope =>
            {
                if (envelope.OriginBusId == _messageBus.Id)
                    _messageBus.PublishEnvelope(envelope);
            }, c => Configure(c, topic, queueName));
            var token = new SubscriptionToken(this, innerToken);
            _tokens.Add(token);
            return token;
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

        private void Unsubscribe(IDisposable token)
        {
            if (_disposing)
                return;
            _tokens.Remove(token);
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
            _disposing = true;
            foreach (var token in _tokens)
                token.Dispose();
            _tokens.Clear();
            _bus.SafeDispose();
        }

        private class SubscriptionToken : IDisposable
        {
            private readonly RabbitModule _module;
            private readonly ISubscriptionResult _token;

            public SubscriptionToken(RabbitModule module, ISubscriptionResult token)
            {
                _token = token;
                _module = module;

            }

            public void Dispose()
            {
                _module.Unsubscribe(this);
                _token.Dispose();
            }
        }
    }
}
