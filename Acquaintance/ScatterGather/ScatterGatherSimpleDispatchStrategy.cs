using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance.ScatterGather
{
    public class ScatterGatherSimpleDispatchStrategy : IScatterGatherChannelDispatchStrategy
    {
        private readonly ConcurrentDictionary<string, IScatterGatherChanne> _reqResChannels;

        public ScatterGatherSimpleDispatchStrategy()
        {
            _reqResChannels = new ConcurrentDictionary<string, IScatterGatherChanne>();
        }

        public IScatterGatherChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name)
        {
            string key = GetReqResKey(typeof(TRequest), typeof(TResponse), name);
            if (!_reqResChannels.ContainsKey(key))
            {
                IScatterGatherChannel<TRequest, TResponse> newChannel = CreateChannel<TRequest, TResponse>();
                _reqResChannels.TryAdd(key, newChannel);
            }
            var channel = _reqResChannels[key] as IScatterGatherChannel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Channel is missing or has incorrect type");
            return channel;
        }

        public IEnumerable<IScatterGatherChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string name)
        {
            string key = GetReqResKey(typeof(TRequest), typeof(TResponse), name);
            if (!_reqResChannels.ContainsKey(key))
                return Enumerable.Empty<IScatterGatherChannel<TRequest, TResponse>>();
            var channel = _reqResChannels[key] as IScatterGatherChannel<TRequest, TResponse>;
            if (channel == null)
                return Enumerable.Empty<IScatterGatherChannel<TRequest, TResponse>>();
            return new[] { channel };
        }

        public void Dispose()
        {
            foreach (var channel in _reqResChannels.Values)
                channel.Dispose();
            _reqResChannels.Clear();
        }

        private IScatterGatherChannel<TRequest, TResponse> CreateChannel<TRequest, TResponse>()
        {
            return new ScatterGatherChannel<TRequest, TResponse>();
        }

        private static string GetReqResKey(Type requestType, Type responseType, string name)
        {
            return $"Request={requestType.AssemblyQualifiedName}:Response={responseType.AssemblyQualifiedName}:Name={name ?? string.Empty}";
        }
    }
}
