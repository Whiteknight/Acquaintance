using System;
using Acquaintance.PubSub;
using Acquaintance.Utility;
using EasyNetQ;

namespace Acquaintance.RabbitMq
{
    public class RabbitSenderBuilder<TPayload> : IRabbitSenderBuilder<TPayload>
    {
        protected readonly IBus _bus;
        protected readonly RabbitModule _module;
        private string _queueName;
        private string _remoteTopic;
        private int _expirationMs;
        private byte _priority;
        private IRabbitSenderBuildStrategy _strategy;

        public RabbitSenderBuilder(IMessageBus messageBus, IBus bus, RabbitModule module)
        {
            Assert.ArgumentNotNull(messageBus, nameof(messageBus));
            Assert.ArgumentNotNull(bus, nameof(bus));

            _bus = bus;
            _module = module;
            _strategy = new DefaultSenderBuildStrategy(messageBus.Id);
        }

        public ISubscriberReference<TPayload> Build()
        {
            var options = BuildOptionsObject();

            return _strategy.Build(_bus, options);
        }

        protected RabbitSenderOptions BuildOptionsObject()
        {
            return new RabbitSenderOptions
            {
                MessagePriority = _priority,
                MessageExpirationMs = _expirationMs < 0 ? 0 : _expirationMs,
                QueueName = _queueName,
                RemoteTopic = _remoteTopic ?? ""
            };
        }

        public IRabbitSenderBuilder<TPayload> UseDefaultQueue()
        {
            _queueName = null;
            return this;
        }

        public IRabbitSenderBuilder<TPayload> UseSpecificQueue(string queueName)
        {
            _queueName = queueName;
            return this;
        }

        public IRabbitSenderBuilder<TPayload> WithRemoteTopic(string remoteTopic)
        {
            _remoteTopic = remoteTopic;
            return this;
        }

        public IRabbitSenderBuilder<TPayload> WithMessageExpiration(int expirationMs)
        {
            _expirationMs = expirationMs;
            return this;
        }

        public IRabbitSenderBuilder<TPayload> WithMessagePriority(byte priority)
        {
            _priority = priority;
            return this;
        }

        public IRabbitSenderBuilder<TPayload> TransformTo<TRemote>(Func<Envelope<TPayload>, string, TRemote> transform)
            where TRemote : class
        {
            _strategy = new TransformingSenderBuildStrategy<TRemote>(transform);
            return this;
        }

        private interface IRabbitSenderBuildStrategy
        {
            ISubscriberReference<TPayload> Build(IBus _bus, RabbitSenderOptions options);
        }

        private class DefaultSenderBuildStrategy : IRabbitSenderBuildStrategy
        {
            private readonly string _messageBusId;

            public DefaultSenderBuildStrategy(string messageBusId)
            {
                _messageBusId = messageBusId;
            }

            public ISubscriberReference<TPayload> Build(IBus _bus, RabbitSenderOptions options)
            {
                return new RabbitForwardingSubscriberReference<TPayload, RabbitEnvelope<TPayload>>(_bus, options, (e, t) => RabbitEnvelope<TPayload>.Wrap(_messageBusId, e, t));
            }
        }

        private class TransformingSenderBuildStrategy<TRemote> : IRabbitSenderBuildStrategy
            where TRemote : class
        {
            private readonly Func<Envelope<TPayload>, string, TRemote> _transform;

            public TransformingSenderBuildStrategy(Func<Envelope<TPayload>, string, TRemote> transform)
            {
                _transform = transform;
            }

            public ISubscriberReference<TPayload> Build(IBus _bus, RabbitSenderOptions options)
            {
                return new RabbitForwardingSubscriberReference<TPayload, TRemote>(_bus, options, _transform);
            }
        }
    }
}