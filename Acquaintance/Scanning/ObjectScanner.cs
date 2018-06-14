using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.Scanning
{
    public class ObjectScanner
    {
        private readonly SubscriptionScanner _subscriptions;
        private readonly ListenerScanner _listeners;

        public ObjectScanner(IMessageBus messageBus, ILogger logger)
        {
            _subscriptions = new SubscriptionScanner(messageBus, logger);
            _listeners = new ListenerScanner(messageBus, logger);
        }

        public IDisposable DetectAndWireUp(object obj, bool useWeakReferences = false)
        {
            var tokens = DetectAndWireUpAll(obj, useWeakReferences);
            return new DisposableCollection(tokens);
        }

        public IEnumerable<IDisposable> DetectAndWireUpAll(object obj, bool useWeakReferences = false)
        {
            var subscriptions = _subscriptions.DetectAndWireUpAll(obj, useWeakReferences);
            var listeners = _listeners.DetectAndWireUpAll(obj, useWeakReferences);
            return subscriptions.Concat(listeners);
        }
    }
}