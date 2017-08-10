using Acquaintance.PubSub;
using System;

namespace Acquaintance.Nets
{
    internal class NodeRepublishSubscriptionHandler<TPayload> : ISubscriptionHandler<TPayload>
    {
        private readonly ISubscriptionHandler<TPayload> _inner;
        private readonly IPubSubBus _messageBus;
        private readonly string _nodeKey;
        private readonly string _outputTopic;
        private readonly string _errorTopic;

        public NodeRepublishSubscriptionHandler(ISubscriptionHandler<TPayload> inner, IPubSubBus messageBus, string nodeKey, string outputTopic, string errorTopic)
        {
            _inner = inner;
            _messageBus = messageBus;
            _nodeKey = nodeKey;
            _outputTopic = outputTopic;
            _errorTopic = errorTopic;
        }

        public void Handle(Envelope<TPayload> message)
        {
            try
            {
                _inner.Handle(message);
                message = message.RedirectToTopic(_outputTopic);
                _messageBus.PublishEnvelope(message);
            }
            catch (Exception e)
            {
                _messageBus.Publish(_errorTopic, new NodeErrorMessage<TPayload>(_nodeKey, message.Payload, e));
            }
        }
    }
}
