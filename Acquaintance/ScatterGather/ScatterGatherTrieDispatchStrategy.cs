using Acquaintance.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.Logging;

namespace Acquaintance.ScatterGather
{
    public class ScatterGatherTrieDispatchStrategy : IScatterGatherChannelDispatchStrategy
    {
        private readonly StringTrie<IScatterGatherChannel> _channels;

        public ScatterGatherTrieDispatchStrategy()
        {
            _channels = new StringTrie<IScatterGatherChannel>();
        }

        public IScatterGatherChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string topic, ILogger log)
        {
            topic = topic ?? string.Empty;
            var channel = _channels.GetOrInsert(typeof(TRequest).FullName, typeof(TResponse).FullName, topic.Split('.'), () => CreateChannel<TRequest, TResponse>(log)) as IScatterGatherChannel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Channel has incorrect type");
            return channel;
        }

        public IEnumerable<IScatterGatherChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string topic)
        {
            topic = topic ?? string.Empty;
            return _channels.Get(typeof(TRequest).FullName, typeof(TResponse).FullName, topic.Split('.')).OfType<IScatterGatherChannel<TRequest, TResponse>>();
        }

        public void Dispose()
        {
            _channels.OnEach(c => c?.Dispose());
        }

        private IScatterGatherChannel<TRequest, TResponse> CreateChannel<TRequest, TResponse>(ILogger log)
        {
            return new ScatterGatherChannel<TRequest, TResponse>(log);
        }
    }
}