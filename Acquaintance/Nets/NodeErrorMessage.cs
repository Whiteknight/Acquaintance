﻿using System;

namespace Acquaintance.Nets
{
    /// <summary>
    /// Message type to hold error information from a Net Node.
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
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
