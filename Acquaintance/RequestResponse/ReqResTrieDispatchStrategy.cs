using Acquaintance.Utility;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

        public IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name, ILogger log)
        {
            name = name ?? string.Empty;
            var channel = _channels.GetOrInsert(typeof(TRequest).FullName, typeof(TResponse).FullName, name.Split('.'), () => CreateChannel<TRequest, TResponse>(log)) as IReqResChannel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Channel has incorrect type");
            return channel;
        }

        public IReqResChannel<TRequest, TResponse> GetExistingChannel<TRequest, TResponse>(string name)
        {
            name = name ?? string.Empty;
            return _channels.Get(typeof(TRequest).FullName, typeof(TResponse).FullName, name.Split('.'))
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