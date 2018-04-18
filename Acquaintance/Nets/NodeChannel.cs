using System;

namespace Acquaintance.Nets
{
    public class NodeChannel
    {
        public NodeChannel(Type type, string topic)
        {
            Type = type;
            Topic = topic;
        }

        public Type Type { get; }
        public string Topic { get; }

        public override string ToString()
        {
            return $"Type={Type.FullName} Topic={Topic}";
        }
    }
}