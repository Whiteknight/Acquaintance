using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.PubSub
{
    public class SimpleDispatchStrategy : IPubSubChannelDispatchStrategy
    {
        private readonly ConcurrentDictionary<string, IPubSubChannel> _pubSubChannels;

        public SimpleDispatchStrategy()
        {
            _pubSubChannels = new ConcurrentDictionary<string, IPubSubChannel>();
        }

        private string GetPubSubKey(Type type, string name)
        {
            return $"Type={type.AssemblyQualifiedName}:Name={name ?? string.Empty}";
        }

        public IPubSubChannel<TPayload> GetChannelForSubscription<TPayload>(string name)
        {
            string key = GetPubSubKey(typeof(TPayload), name);
            if (!_pubSubChannels.ContainsKey(key))
                _pubSubChannels.TryAdd(key, new PubSubChannel<TPayload>());
            var channel = _pubSubChannels[key] as IPubSubChannel<TPayload>;
            if (channel == null)
                throw new Exception("Channel has incorrect type");
            return channel;
        }

        public IEnumerable<IPubSubChannel<TPayload>> GetExistingChannels<TPayload>(string name)
        {
            string key = GetPubSubKey(typeof(TPayload), name);
            if (!_pubSubChannels.ContainsKey(key))
                return Enumerable.Empty<IPubSubChannel<TPayload>>();
            var channel = _pubSubChannels[key] as IPubSubChannel<TPayload>;
            if (channel == null)
                return Enumerable.Empty<IPubSubChannel<TPayload>>();
            return new[] { channel };
        }

        public void Dispose()
        {
            foreach (var channel in _pubSubChannels.Values)
                channel.Dispose();
            _pubSubChannels.Clear();
        }
    }
}