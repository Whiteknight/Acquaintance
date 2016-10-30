using Acquaintance.PubSub;
using Acquaintance.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Dispatching
{
    public class SimplePubSubChannelDispatchStrategy : IPubSubChannelDispatchStrategy
    {
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly Dictionary<string, IPubSubChannel> _pubSubChannels;

        public SimplePubSubChannelDispatchStrategy(MessagingWorkerThreadPool threadPool)
        {
            _threadPool = threadPool;
            _pubSubChannels = new Dictionary<string, IPubSubChannel>();
        }

        private string GetPubSubKey(Type type, string name)
        {
            return string.Format("Type={0}:Name={1}", type.AssemblyQualifiedName, name ?? string.Empty);
        }

        public IPubSubChannel<TPayload> GetChannelForSubscription<TPayload>(string name)
        {
            string key = GetPubSubKey(typeof(TPayload), name);
            if (!_pubSubChannels.ContainsKey(key))
                _pubSubChannels.Add(key, new PubSubChannel<TPayload>(_threadPool));
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