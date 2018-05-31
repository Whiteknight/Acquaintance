using System;
using System.Collections.Generic;
using Acquaintance.Outbox;
using Acquaintance.PubSub;
using Acquaintance.Utility;
using EasyNetQ;
using EasyNetQ.FluentConfiguration;

namespace Acquaintance.RabbitMq
{
    public class RabbitModule : IMessageBusModule, IDisposable
    {
        private readonly IBus _bus;
        private readonly HashSet<IDisposable> _tokens;
        private readonly IMessageBus _messageBus;
        private bool _disposing;

        public RabbitModule(IMessageBus messageBus, string connectionString)
        {
            _messageBus = messageBus;
            _bus = RabbitHutch.CreateBus(connectionString);
            _tokens = new HashSet<IDisposable>();
            _disposing = false;
            
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public IDisposable SubscribeRemote<TPayload>(string[] topics)
        {
            var queueName = MakeQueueName<TPayload>();
            if (topics == null)
            {
                string id = CreateSubscriberId();
                var token = SubscribeRemoteInternal<TPayload>(id, queueName, "#");
                _tokens.Add(token);
                return token;
            }

            topics = TopicUtility.CanonicalizeTopics(topics);

            var tokens = new DisposableCollection();
            foreach (var topic in topics)
            {
                string id = CreateSubscriberId();
                var token = SubscribeRemoteInternal<TPayload>(id, queueName, topic);
                _tokens.Add(token);
                tokens.Add(token);
            }
            return tokens;
        }

        private SubscriptionToken SubscribeRemoteInternal<TPayload>(string id, string queueName, string topic)
        {
            var innerToken = _bus.Subscribe<Envelope<TPayload>>(id, envelope =>
            {
                if (envelope.OriginBusId == _messageBus.Id)
                    _messageBus.PublishEnvelope(envelope);
            }, c => Configure(c, topic, queueName));
            var token = new SubscriptionToken(this, innerToken);
            return token;
        }

        public ISubscription<TPayload> CreateForwardingSubscriber<TPayload>(IOutbox<TPayload> outbox = null)
        {
            var queueName = MakeQueueName<TPayload>();
            return new ForwardToRabbitSubscription<TPayload>(_messageBus, _bus, queueName, outbox ?? new InMemoryOutbox<TPayload>(100));
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
            // TODO: Make more of these options available to the subscriber
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
