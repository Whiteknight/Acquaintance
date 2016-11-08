using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.RequestResponse
{
    public class ReqResSimpleDispatchStrategy : IReqResChannelDispatchStrategy
    {
        private readonly ConcurrentDictionary<string, IReqResChannel> _reqResChannels;
        private readonly bool _isExclusive;

        public ReqResSimpleDispatchStrategy(bool isExclusive)
        {
            _reqResChannels = new ConcurrentDictionary<string, IReqResChannel>();
            _isExclusive = isExclusive;
        }

        public IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name)
        {
            string key = GetReqResKey(typeof(TRequest), typeof(TResponse), name);
            if (!_reqResChannels.ContainsKey(key))
            {
                IReqResChannel<TRequest, TResponse> newChannel = CreateChannel<TRequest, TResponse>();
                _reqResChannels.TryAdd(key, newChannel);
            }
            var channel = _reqResChannels[key] as IReqResChannel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Channel is missing or has incorrect type");
            return channel;
        }

        public IEnumerable<IReqResChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string name)
        {
            string key = GetReqResKey(typeof(TRequest), typeof(TResponse), name);
            if (!_reqResChannels.ContainsKey(key))
                return Enumerable.Empty<IReqResChannel<TRequest, TResponse>>();
            var channel = _reqResChannels[key] as IReqResChannel<TRequest, TResponse>;
            if (channel == null)
                return Enumerable.Empty<IReqResChannel<TRequest, TResponse>>();
            return new[] { channel };
        }

        public void Dispose()
        {
            foreach (var channel in _reqResChannels.Values)
                channel.Dispose();
            _reqResChannels.Clear();
        }

        private IReqResChannel<TRequest, TResponse> CreateChannel<TRequest, TResponse>()
        {
            if (_isExclusive)
                return new RequestResponseChannel<TRequest, TResponse>();
            else
                return new ScatterGatherChannel<TRequest, TResponse>();
        }

        private static string GetReqResKey(Type requestType, Type responseType, string name)
        {
            return $"Request={requestType.AssemblyQualifiedName}:Response={responseType.AssemblyQualifiedName}:Name={name ?? string.Empty}";
        }
    }
}
