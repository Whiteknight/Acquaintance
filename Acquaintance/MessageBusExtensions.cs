using System;
using Acquaintance.Scanning;

namespace Acquaintance
{
    public static class MessageBusExtensions
    {
        public static IDisposable AutoWireup(this IMessageBus messageBus, object obj, bool useWeakReferences = false)
        {
            return new ObjectScanner(messageBus, messageBus.Logger).DetectAndWireUp(obj, useWeakReferences);
        }
    }
}
