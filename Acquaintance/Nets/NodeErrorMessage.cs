using System;

namespace Acquaintance.Nets
{
    public class NodeErrorMessage<TPayload>
    {
        public NodeErrorMessage(string nodeKey, TPayload payload, Exception error)
        {
            NodeKey = nodeKey;
            Payload = payload;
            Error = error;
        }

        public string NodeKey { get; private set; }
        public TPayload Payload { get; private set; }
        public Exception Error { get; private set; }
    }
}
