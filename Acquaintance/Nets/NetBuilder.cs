using System;
using System.Collections.Generic;

namespace Acquaintance.Nets
{
    /// <summary>
    /// Builder type to construct a new Net
    /// </summary>
    public class NetBuilder
    {
        private readonly Dictionary<string, NodeBuilder> _nodes;
        private readonly IMessageBus _messageBus;

        public NetBuilder()
        {
            _nodes = new Dictionary<string, NodeBuilder>();
            _messageBus = new MessageBus();
        }

        public Net BuildNet()
        {
            var inputs = new Dictionary<string,  IReadOnlyList<NodeChannel>>();
            var outputs = new Dictionary<string, IReadOnlyList<NodeChannel>>();
            foreach (var kvp in _nodes)
            {
                var name = kvp.Key;
                var nodeBuilder = kvp.Value;
                nodeBuilder.BuildToMessageBus();
                inputs.Add(name, nodeBuilder.GetReadChannels());
                outputs.Add(name, nodeBuilder.GetWriteChannels());
            }

            return new Net(_messageBus, inputs, outputs);
        }

        public NodeToken AddNode<T>(string name, Action<INodeBuilderReader<T>> setup)
        {
            return AddNodeInternal(name, false, setup);
        }

        public NodeToken AddErrorNode<T>(string name, Action<INodeBuilderReader<NodeErrorMessage<T>>> setup)
        {
            return AddNodeInternal(name, true, setup);
        }

        private NodeToken AddNodeInternal<T>(string name, bool readErrors, Action<INodeBuilderReader<T>> setup)
        {
            string key = name.ToLowerInvariant();
            if (_nodes.ContainsKey(key))
                throw new Exception("Cannot add new node with same name as existing node");
            var builder = new NodeBuilder<T>(key, _messageBus, readErrors);
            setup(builder);
            _nodes.Add(key, builder);
            return new NodeToken(name);
        }
    }

    public class NodeToken
    {
        public NodeToken(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
