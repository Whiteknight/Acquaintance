using Acquaintance.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Logging;

namespace Acquaintance.PubSub
{
    public class PubSubTrieDispatchStrategy : IPubSubChannelDispatchStrategy
    {
        private readonly StringTrie<IPubSubChannel> _channels;

        public PubSubTrieDispatchStrategy()
        {
            _channels = new StringTrie<IPubSubChannel>();
        }

        public IPubSubChannel<TPayload> GetChannelForSubscription<TPayload>(string topic, ILogger log)
        {
            topic = topic ?? string.Empty;
            var channel = _channels.GetOrInsert(typeof(TPayload).FullName, topic.Split('.'), () => new PubSubChannel<TPayload>(log)) as IPubSubChannel<TPayload>;
            if (channel == null)
                throw new Exception("Channel has incorrect type");
            return channel;
        }

        public IEnumerable<IPubSubChannel<TPayload>> GetExistingChannels<TPayload>(string topic)
        {
            topic = topic ?? string.Empty;
            return _channels.Get(typeof(TPayload).FullName, topic.Split('.')).OfType<IPubSubChannel<TPayload>>();
        }

        public void Dispose()
        {
            _channels.OnEach(c => c?.Dispose());
        }
    }
}