﻿using Acquaintance.PubSub;
using System;

namespace Acquaintance.Nets
{
    public class NodeRepublishSubscriptionHandler<TPayload> : ISubscriptionHandler<TPayload>
    {
        private readonly ISubscriptionHandler<TPayload> _inner;
        private readonly IPublishable _messageBus;
        private readonly string _nodeKey;
        private readonly string _outputChannelName;
        private readonly string _errorChannelName;

        public NodeRepublishSubscriptionHandler(ISubscriptionHandler<TPayload> inner, IPublishable messageBus, string nodeKey, string outputChannelName, string errorChannelName)
        {
            _inner = inner;
            _messageBus = messageBus;
            _nodeKey = nodeKey;
            _outputChannelName = outputChannelName;
            _errorChannelName = errorChannelName;
        }

        public void Handle(TPayload payload)
        {
            try
            {
                _inner.Handle(payload);
                _messageBus.Publish(_outputChannelName, payload);
            }
            catch (Exception e)
            {
                _messageBus.Publish(_errorChannelName, new NodeErrorMessage<TPayload>(_nodeKey, payload, e));
            }
        }
    }
}