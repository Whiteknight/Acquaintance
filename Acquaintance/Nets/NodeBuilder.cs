using Acquaintance.PubSub;
using System;
using System.Collections.Generic;

namespace Acquaintance.Nets
{
    public class NodeBuilder<TInput> : INodeBuilder
    {
        private Action<TInput> _action;
        private Func<TInput, bool> _predicate;
        private string _channelName;
        private readonly IMessageBus _messageBus;
        private readonly string _key;

        // TODO: Maybe support some of the features from SubscriptionBuilder such as MaxEvents
        // and thread affinity

        public NodeBuilder(string key, IMessageBus messageBus)
        {
            if (messageBus == null)
                throw new ArgumentNullException(nameof(messageBus));
            _messageBus = messageBus;
            _key = key;
        }

        private static string OutputChannelName(string key)
        {
            return "OutputFrom_" + key;
        }

        void INodeBuilder.BuildToMessageBus()
        {
            if (_action == null)
                throw new Exception("No action provided");

            _messageBus.Subscribe<TInput>(b1 =>
            {
                IActionSubscriptionBuilder<TInput> b2;
                if (string.IsNullOrEmpty(_channelName))
                    b2 = b1.OnDefaultChannel();
                else
                    b2 = b1.WithChannelName(_channelName);

                var b3 = b2.InvokeAction(_action).OnWorkerThread();

                if (_predicate != null)
                    b3 = b3.WithFilter(_predicate);
            });
        }

        public NodeBuilder<TInput> Transform<TOut>(Func<TInput, TOut> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_action != null)
                throw new Exception("Node already has a handler defined");

            string outputChannelName = OutputChannelName(_key);
            _action = e =>
            {
                var result = handler(e);
                _messageBus.Publish(outputChannelName, result);
            };
            return this;
        }

        public NodeBuilder<TInput> TransformMany<TOut>(Func<TInput, IEnumerable<TOut>> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_action != null)
                throw new Exception("Node already has a handler defined");

            string outputChannelName = OutputChannelName(_key);
            _action = e =>
            {
                var results = handler(e);
                foreach (var r in results)
                    _messageBus.Publish(outputChannelName, r);
            };
            return this;
        }

        public NodeBuilder<TInput> Handle(Action<TInput> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_action != null)
                throw new Exception("Node already has a handler defined");
            _action = handler;
            return this;
        }

        public NodeBuilder<TInput> ReadInput()
        {
            if (!string.IsNullOrEmpty(_channelName))
                throw new Exception("Node can only read from a single input");
            _channelName = Net.NetworkInputChannelName;
            return this;
        }

        public NodeBuilder<TInput> ReadFrom(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
                throw new ArgumentNullException(nameof(nodeName));
            if (!string.IsNullOrEmpty(_channelName))
                throw new Exception("Node can only read from a single input");

            string key = nodeName.ToLowerInvariant();
            string outputChannelName = OutputChannelName(key);
            _channelName = outputChannelName;
            return this;
        }

        public NodeBuilder<TInput> OnCondition(Func<TInput, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (_predicate != null)
                throw new Exception("Node can only have a single predicate");
            _predicate = predicate;
            return this;
        }
    }
}
