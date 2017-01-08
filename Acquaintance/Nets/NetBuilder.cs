using System;
using System.Collections.Generic;

namespace Acquaintance.Nets
{

    public class NetBuilder
    {
        private Dictionary<string, INodeBuilder> _nodes;
        private readonly IMessageBus _messageBus;

        public NetBuilder()
        {
            _nodes = new Dictionary<string, INodeBuilder>();
            _messageBus = new MessageBus();
        }

        public Net BuildNet()
        {
            foreach (var kvp in _nodes)
                kvp.Value.BuildToMessageBus();
            return new Nets.Net(_messageBus);
        }

        public NodeBuilder<T> AddNode<T>(string name)
        {
            string key = name.ToLowerInvariant();
            if (_nodes.ContainsKey(key))
                throw new System.Exception("Cannot add new node with same name as existing node");
            var builder = new NodeBuilder<T>(key, _messageBus);
            _nodes.Add(key, builder);
            return builder;
        }
    }
}
