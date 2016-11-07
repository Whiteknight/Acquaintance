using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Acquaintance.Testing
{
    [Serializable]
    public class ExpectationFailedException : Exception
    {
        public ExpectationFailedException()
        {
        }

        public ExpectationFailedException(string message)
            : base(message)
        {
        }

        public ExpectationFailedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public ExpectationFailedException(IEnumerable<string> messages)
            : base("Missing expected messages: " + string.Join("\n", messages))
        {
        }

        protected ExpectationFailedException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}