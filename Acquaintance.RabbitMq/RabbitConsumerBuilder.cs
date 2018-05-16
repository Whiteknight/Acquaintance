using System;
using EasyNetQ;

namespace Acquaintance.RabbitMq
{
    // TODO: Subscribe to many topics
    public class RabbitConsumerBuilder<TPayload> :
        IRabbitConsumerBuilderRemoteTopic<TPayload>,
        IRabbitConsumerBuilderRemoteMessage<TPayload>,
        IRabbitConsumerBuilderLocalTopic<TPayload>,
        IRabbitConsumerBuilderQueue<TPayload>,
        IRabbitConsumerBuilderDetails<TPayload>
    {
        private readonly IBus _bus;
        private readonly IMessageBus _messageBus;
        private readonly RabbitModule _module;

        private bool _receiveRaw;
        private string _remoteTopic;
        private Func<string, string> _makeLocalTopic;
        private string _queueName;
        private int? _expirationMs;
        private bool _remoteOnly;
        private IConsumerBuildStrategy _strategy;

        public RabbitConsumerBuilder(IBus bus, IMessageBus messageBus, RabbitModule module)
        {
            _bus = bus;
            _messageBus = messageBus;
            _module = module;
            _receiveRaw = false;
            _remoteOnly = false;
        }

        public RabbitConsumer Build()
        {
            if (_strategy == null)
                throw new Exception("Must specify a message payload format");
            var options = GetOptionsObject();
            var rabbitToken = _strategy.SubscribeConsumer(_messageBus, options);
            return new RabbitConsumer(_messageBus, _strategy, options, rabbitToken);
        }

        private RabbitConsumerOptions GetOptionsObject()
        {
            return new RabbitConsumerOptions
            {
                MakeLocalTopic = GetLocalTopicBuilder(),
                RemoteTopic = _remoteTopic ?? "#",
                QueueExpirationMs = _expirationMs,
                QueueName = _queueName,
                RawMessages = _receiveRaw,
                RemoteOnly = _remoteOnly
            };
        }

        private Func<string, string> GetLocalTopicBuilder()
        {
            if (_makeLocalTopic != null)
                return _makeLocalTopic;
            if (_remoteTopic != null)
                return s => _remoteTopic;
            return s => "";
        }

        public IRabbitConsumerBuilderRemoteMessage<TPayload> WithRemoteTopic(string remoteTopic)
        {
            _remoteTopic = remoteTopic;
            if (_remoteTopic == null)
                _remoteTopic = "#";
            return this;
        }

        public IRabbitConsumerBuilderRemoteMessage<TPayload> ForAllRemoteTopics()
        {
            _remoteTopic = "#";
            return this;
        }

        public IRabbitConsumerBuilderLocalTopic<TPayload> ReceiveDefaultFormat()
        {
            _receiveRaw = false;
            _strategy = new DefaultConsumerBuildStrategy<TPayload>(_bus);
            return this;
        }

        public IRabbitConsumerBuilderLocalTopic<TPayload> ReceiveRawMessage()
        {
            _receiveRaw = true;
            return this;
        }

        public IRabbitConsumerBuilderLocalTopic<TPayload> TransformFrom<TRemote>(Func<TRemote, string, Envelope<TPayload>> transform, Func<TRemote, string> getTopic = null)
            where TRemote : class
        {
            _strategy = new TransformingConsumerBuildStrategy<TPayload, TRemote>(_bus, transform, getTopic);
            return this;
        }

        public IRabbitConsumerBuilderQueue<TPayload> ForwardToLocalTopic(string localTopic)
        {
            _makeLocalTopic = s => localTopic;
            return this;
        }

        public IRabbitConsumerBuilderQueue<TPayload> ForwardToLocalTopic(Func<string, string> transformTopic)
        {
            _makeLocalTopic = transformTopic;
            return this;
        }

        public IRabbitConsumerBuilderDetails<TPayload> UseUniqueQueue()
        {
            _queueName = _module.MakeInstanceUniqueQueueName<TPayload>();
            return this;
        }

        public IRabbitConsumerBuilderDetails<TPayload> UseSharedQueueName()
        {
            _queueName = _module.MakeSharedQueueName<TPayload>();
            return this;
        }

        public IRabbitConsumerBuilderDetails<TPayload> UseQueueName(string queueName)
        {
            _queueName = queueName;
            return this;
        }

        public IRabbitConsumerBuilderDetails<TPayload> AutoExpireQueue()
        {
            _expirationMs = 1000;
            return this;
        }

        public IRabbitConsumerBuilderDetails<TPayload> ExpireQueue(int expirationMs)
        {
            _expirationMs = expirationMs;
            return this;
        }

        public IRabbitConsumerBuilderDetails<TPayload> ReceiveRemoteMessagesOnly()
        {
            _remoteOnly = true;
            return this;
        }
    }
}