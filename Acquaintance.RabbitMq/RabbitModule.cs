using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;
using EasyNetQ;

namespace Acquaintance.RabbitMq
{
    // TODO: Support Request/Response
    // TODO: Support Scatter/Gather
    // TODO: Handle Connected/Disconnected and Blocked/Unblocked events
    // TODO: Make sure we create all necessary queues on reconnect, including queues which have expired and queues which could not be created
    public class RabbitModule : IMessageBusModule, IDisposable
    {
        private readonly IBus _bus;
        private readonly HashSet<IDisposable> _tokens;
        private readonly IMessageBus _messageBus;
        private readonly ConcurrentDictionary<Guid, RabbitConsumer> _receivers;
        private bool _disposing;

        public RabbitModule(IMessageBus messageBus, string connectionString)
        {
            _messageBus = messageBus;
            _bus = RabbitHutch.CreateBus(connectionString, register => 
            {
                register.Register<ITypeNameSerializer>(p => new RabbitTypeNameSerializer());
            });
            _bus.Advanced.Disconnected += OnRabbitDisconnected;
            _bus.Advanced.Connected += OnRabbitConnected;
            _tokens = new HashSet<IDisposable>();
            _disposing = false;
            _receivers = new ConcurrentDictionary<Guid, RabbitConsumer>();
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public IDisposable ManageConsumer(RabbitConsumer consumer)
        {
            var tokenId = Guid.NewGuid();
            if (!_receivers.TryAdd(tokenId, consumer))
                throw new Exception("Could not add subscriber");

            var token = new SubscriptionToken(this, consumer.RabbitToken, tokenId, consumer.Queue.QueueName, consumer.Queue.RemoteTopic);
            _tokens.Add(token);
            return token;
        }

        public RabbitSenderBuilder<TPayload> CreatePublisherBuilder<TPayload>()
        {
            return new RabbitSenderBuilder<TPayload>(_messageBus, _bus, this);
        }

        public RabbitConsumerBuilder<TPayload> CreateSubscriberBuilder<TPayload>()
        {
            return new RabbitConsumerBuilder<TPayload>(_bus, _messageBus, this);
        }

        public string MakeSharedQueueName<TPayload>()
        {
            var t = typeof(TPayload);
            return $"AQ:{t.Namespace}.{t.Name}";
        }

        public string MakeInstanceUniqueQueueName<TPayload>()
        {
            var t = typeof(TPayload);
            return $"AQ:{t.Namespace}.{t.Name}:{_messageBus.Id}";
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        public Envelope<TResponse> Request<TRequest, TResponse>(Envelope<TRequest> request)
        {
            var rabbitRequest = RabbitEnvelope<TRequest>.Wrap(_messageBus.Id, request, request.Topics.FirstOrDefault() ?? "");
            var rabbitResponse = _bus.Request<RabbitEnvelope<TRequest>, RabbitEnvelope<TResponse>>(rabbitRequest);
            return _messageBus.EnvelopeFactory.CreateFromRemote(rabbitResponse.OriginBusId, null, rabbitResponse.Payload, rabbitResponse.Metadata);
        }

        public IDisposable Listen<TRequest, TResponse>(Func<Envelope<TRequest>, TResponse> handle)
            where TResponse : class
        {
            return _bus.Respond<RabbitEnvelope<TRequest>, TResponse>(request =>
            {
                var envelope = _messageBus.EnvelopeFactory.CreateFromRemote(request.OriginBusId, request.Topics, request.Payload, request.Metadata);
                return handle(envelope);
            });
        }

        ~RabbitModule()
        {
            Dispose(false);
        }

        private void OnRabbitDisconnected(object o, EventArgs args)
        {
            _messageBus.Logger.Warn("RabbitMQ has disconnected");
        }

        private void OnRabbitConnected(object o, EventArgs args)
        {
            _messageBus.Logger.Info("RabbitMQ has reconnected");
            EnsureObjectsExist();
        }

        private void EnsureObjectsExist()
        {
            foreach (var receiver in _receivers.Values)
                receiver.Reconnect();
        }

        private void Unsubscribe(IDisposable token, Guid id)
        {
            if (_disposing)
                return;
            _tokens.Remove(token);
            _receivers.TryRemove(id, ObjectManagement.TryDispose);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;
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
            private readonly Guid _id;
            private readonly string _queueName;
            private readonly string _topic;

            public SubscriptionToken(RabbitModule module, ISubscriptionResult token, Guid id, string queueName, string topic)
            {
                _token = token;
                _id = id;
                _queueName = queueName;
                _topic = topic;
                _module = module;
            }

            public void Dispose()
            {
                _token.Dispose();
                _module.Unsubscribe(this, _id);
            }

            public override string ToString()
            {
                return $"RabbitMQ Subscription for Queue {_queueName} on Topic {_topic}";
            }
        }
    }
}
