using Acquaintance.Utility;
using System;
using System.Linq;
using Acquaintance.Logging;

namespace Acquaintance.RequestResponse
{
    public class ReqResTrieDispatchStrategy : IReqResChannelDispatchStrategy
    {
        private readonly StringTrie<IReqResChannel> _channels;

        public ReqResTrieDispatchStrategy()
        {
            _channels = new StringTrie<IReqResChannel>();
        }

        public IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string topic, ILogger log)
        {
            topic = topic ?? string.Empty;
            var channel = _channels.GetOrInsert(typeof(TRequest).FullName, typeof(TResponse).FullName, topic.Split('.'), () => CreateChannel<TRequest, TResponse>(log)) as IReqResChannel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Channel has incorrect type");
            return channel;
        }

        public IReqResChannel<TRequest, TResponse> GetExistingChannel<TRequest, TResponse>(string topic)
        {
            topic = topic ?? string.Empty;
            return _channels.Get(typeof(TRequest).FullName, typeof(TResponse).FullName, topic.Split('.'))
                .OfType<IReqResChannel<TRequest, TResponse>>()
                .FirstOrDefault();
        }

        public void Dispose()
        {
            _channels.OnEach(c => c?.Dispose());
        }

        private IReqResChannel<TRequest, TResponse> CreateChannel<TRequest, TResponse>(ILogger log)
        {
            return new RequestResponseChannel<TRequest, TResponse>(log);
        }
    }
}