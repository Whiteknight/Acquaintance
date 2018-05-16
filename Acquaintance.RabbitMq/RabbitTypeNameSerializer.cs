using System;
using System.Linq;
using EasyNetQ;

namespace Acquaintance.RabbitMq
{
    public class RabbitTypeNameSerializer : ITypeNameSerializer
    {
        private readonly ITypeNameSerializer _defaultSerializer;

        public RabbitTypeNameSerializer()
        {
            _defaultSerializer = new TypeNameSerializer();
        }

        public string Serialize(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RabbitEnvelope<>))
            {
                var payloadType = type.GetGenericArguments().Single();
                var defaultSerialized = _defaultSerializer.Serialize(payloadType);
                return $"AQ:{defaultSerialized}";
            }

            return _defaultSerializer.Serialize(type);
        }

        public Type DeSerialize(string typeName)
        {
            if (typeName.StartsWith("AQ:"))
            {
                typeName = typeName.Substring(3);
                var payloadType = _defaultSerializer.DeSerialize(typeName);
                return typeof(RabbitEnvelope<>).MakeGenericType(payloadType);
            }

            return _defaultSerializer.DeSerialize(typeName);
        }
    }
}