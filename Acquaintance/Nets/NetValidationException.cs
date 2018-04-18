using System;
using System.Runtime.Serialization;

namespace Acquaintance.Nets
{
    [Serializable]
    public class NetValidationException : Exception
    {
        public NetValidationException()
        {
        }

        public NetValidationException(string message)
            : base(message)
        {
        }

        public NetValidationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected NetValidationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}