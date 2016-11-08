using Acquaintance.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class ReqResTrieDispatchStrategy : IReqResChannelDispatchStrategy
    {
        private readonly StringTrie<IReqResChannel> _channels;
        private readonly bool _isExclusive;

        public ReqResTrieDispatchStrategy(bool isExclusive)
        {
            _channels = new StringTrie<IReqResChannel>();
            _isExclusive = isExclusive;
        }

        public IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name)
        {
            name = name ?? string.Empty;
            var channel = _channels.GetOrInsert(typeof(TRequest).FullName, typeof(TResponse).FullName, name.Split('.'), CreateChannel<TRequest, TResponse>) as IReqResChannel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Channel has incorrect type");
            return channel;
        }

        public IEnumerable<IReqResChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string name)
        {
            name = name ?? string.Empty;
            return _channels.Get(typeof(TRequest).FullName, typeof(TResponse).FullName, name.Split('.')).OfType<IReqResChannel<TRequest, TResponse>>();
        }

        public void Dispose()
        {
            _channels.OnEach(c => c?.Dispose());
        }

        private IReqResChannel<TRequest, TResponse> CreateChannel<TRequest, TResponse>()
        {
            if (_isExclusive)
                return new RequestResponseChannel<TRequest, TResponse>();
            else
                return new ScatterGatherChannel<TRequest, TResponse>();
        }
    }
}