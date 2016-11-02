using Acquaintance.RequestResponse;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.Dispatching
{
    public class SimpleReqResChannelDispatchStrategy : IReqResChannelDispatchStrategy
    {
        private readonly ConcurrentDictionary<string, IReqResChannel> _reqResChannels;

        public SimpleReqResChannelDispatchStrategy()
        {
            _reqResChannels = new ConcurrentDictionary<string, IReqResChannel>();
        }

        public IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name, bool requestExclusivity)
        {
            string key = GetReqResKey(typeof(TRequest), typeof(TResponse), name);
            if (!_reqResChannels.ContainsKey(key))
            {
                IReqResChannel<TRequest, TResponse> newChannel;
                if (requestExclusivity)
                    newChannel = new ExclusiveReqResChannel<TRequest, TResponse>();
                else
                    newChannel = new ReqResChannel<TRequest, TResponse>();

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

        private static string GetReqResKey(Type requestType, Type responseType, string name)
        {
            return $"Request={requestType.AssemblyQualifiedName}:Response={responseType.AssemblyQualifiedName}:Name={name ?? string.Empty}";
        }
    }
}
