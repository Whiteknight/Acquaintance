using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Utility;

namespace Acquaintance.PubSub
{
    public class TrieDispatchStrategy : IPubSubChannelDispatchStrategy
    {
        private readonly StringTrie<IPubSubChannel> _channels;

        public TrieDispatchStrategy()
        {
            _channels = new StringTrie<IPubSubChannel>();
        }

        public IPubSubChannel<TPayload> GetChannelForSubscription<TPayload>(string name)
        {
            name = name ?? string.Empty;
            var channel = _channels.GetOrInsert(typeof(TPayload).FullName, name.Split('.'), () => new PubSubChannel<TPayload>()) as IPubSubChannel<TPayload>;
            if (channel == null)
                throw new Exception("Channel has incorrect type");
            return channel;
        }

        public IEnumerable<IPubSubChannel<TPayload>> GetExistingChannels<TPayload>(string name)
        {
            name = name ?? string.Empty;
            return _channels.Get(typeof(TPayload).FullName, name.Split('.')).OfType<IPubSubChannel<TPayload>>();
        }

        public void Dispose()
        {
            _channels.OnEach(c => c?.Dispose());
        }
    }
}