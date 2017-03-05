using Acquaintance.PubSub;
using System;

namespace Acquaintance.Nets
{
    internal class NodeRepublishSubscriptionHandler<TPayload> : ISubscriptionHandler<TPayload>
    {
        private readonly ISubscriptionHandler<TPayload> _inner;
        private readonly IPubSubBus _messageBus;
        private readonly string _nodeKey;
        private readonly string _outputChannelName;
        private readonly string _errorChannelName;

        public NodeRepublishSubscriptionHandler(ISubscriptionHandler<TPayload> inner, IPubSubBus messageBus, string nodeKey, string outputChannelName, string errorChannelName)
        {
            _inner = inner;
            _messageBus = messageBus;
            _nodeKey = nodeKey;
            _outputChannelName = outputChannelName;
            _errorChannelName = errorChannelName;
        }

        public void Handle(Envelope<TPayload> message)
        {
            try
            {
                _inner.Handle(message);
                message = message.RedirectToChannel(_outputChannelName);
                _messageBus.PublishEnvelope(message);
            }
            catch (Exception e)
            {
                _messageBus.Publish(_errorChannelName, new NodeErrorMessage<TPayload>(_nodeKey, message.Payload, e));
            }
        }
    }
}
