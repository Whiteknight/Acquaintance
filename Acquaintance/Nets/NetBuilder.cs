using System.Collections.Generic;

namespace Acquaintance.Nets
{
    /// <summary>
    /// Builder type to construct a new Net
    /// </summary>
    public class NetBuilder
    {
        private readonly Dictionary<string, INodeBuilder> _nodes;
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
            return new Net(_messageBus);
        }

        public NodeBuilder<T> AddNode<T>(string name)
        {
            return AddNodeInternal<T>(name, false);
        }

        private NodeBuilder<T> AddNodeInternal<T>(string name, bool readErrors)
        {
            string key = name.ToLowerInvariant();
            if (_nodes.ContainsKey(key))
                throw new System.Exception("Cannot add new node with same name as existing node");
            var builder = new NodeBuilder<T>(key, _messageBus, readErrors);
            _nodes.Add(key, builder);
            return builder;
        }

        public NodeBuilder<NodeErrorMessage<T>> AddErrorNode<T>(string name)
        {
            return AddNodeInternal<NodeErrorMessage<T>>(name, true);
        }
    }
}
