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

            _messageBus.Subscribe<TInput>(s =>
            {
                if (!string.IsNullOrEmpty(_channelName))
                    s.WithChannelName(_channelName);
                if (_predicate != null)
                    s.WithFilter(_predicate);
                s.InvokeAction(_action);
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
