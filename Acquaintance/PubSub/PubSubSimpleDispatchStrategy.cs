using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Logging;

namespace Acquaintance.PubSub
{
    public class PubSubSimpleDispatchStrategy : IPubSubChannelDispatchStrategy
    {
        private readonly ConcurrentDictionary<string, IPubSubChannel> _pubSubChannels;

        public PubSubSimpleDispatchStrategy()
        {
            _pubSubChannels = new ConcurrentDictionary<string, IPubSubChannel>();
        }
        
        public IPubSubChannel<TPayload> GetChannelForSubscription<TPayload>(string name, ILogger log)
        {
            string key = GetPubSubKey(typeof(TPayload), name);

            var channel = _pubSubChannels.GetOrAdd(key, k => new PubSubChannel<TPayload>(log));
            var typedChannel = channel as IPubSubChannel<TPayload>;
            if (typedChannel == null)
                throw new Exception("Channel has incorrect type");

            return typedChannel;
        }

        public IEnumerable<IPubSubChannel<TPayload>> GetExistingChannels<TPayload>(string name)
        {
            string key = GetPubSubKey(typeof(TPayload), name);
            if (!_pubSubChannels.ContainsKey(key))
                return Enumerable.Empty<IPubSubChannel<TPayload>>();

            bool ok = _pubSubChannels.TryGetValue(key, out IPubSubChannel channel);
            if (!ok || channel == null)
                return Enumerable.Empty<IPubSubChannel<TPayload>>();

            var typedChannel = channel as IPubSubChannel<TPayload>;
            if (typedChannel == null)
                return Enumerable.Empty<IPubSubChannel<TPayload>>();

            return new[] { typedChannel };
        }

        public void Dispose()
        {
            foreach (var channel in _pubSubChannels.Values)
                channel.Dispose();
            _pubSubChannels.Clear();
        }

        private static string GetPubSubKey(Type type, string name)
        {
            return $"Type={type.AssemblyQualifiedName}:Name={name ?? string.Empty}";
        }
    }
}