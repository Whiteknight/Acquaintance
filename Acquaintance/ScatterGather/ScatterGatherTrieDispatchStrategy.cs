using Acquaintance.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.ScatterGather
{
    public class ScatterGatherTrieDispatchStrategy : IScatterGatherChannelDispatchStrategy
    {
        private readonly StringTrie<IScatterGatherChanne> _channels;

        public ScatterGatherTrieDispatchStrategy()
        {
            _channels = new StringTrie<IScatterGatherChanne>();
        }

        public IScatterGatherChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name)
        {
            name = name ?? string.Empty;
            var channel = _channels.GetOrInsert(typeof(TRequest).FullName, typeof(TResponse).FullName, name.Split('.'), CreateChannel<TRequest, TResponse>) as IScatterGatherChannel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Channel has incorrect type");
            return channel;
        }

        public IEnumerable<IScatterGatherChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string name)
        {
            name = name ?? string.Empty;
            return _channels.Get(typeof(TRequest).FullName, typeof(TResponse).FullName, name.Split('.')).OfType<IScatterGatherChannel<TRequest, TResponse>>();
        }

        public void Dispose()
        {
            _channels.OnEach(c => c?.Dispose());
        }

        private IScatterGatherChannel<TRequest, TResponse> CreateChannel<TRequest, TResponse>()
        {
            return new ScatterGatherChannel<TRequest, TResponse>();
        }
    }
}