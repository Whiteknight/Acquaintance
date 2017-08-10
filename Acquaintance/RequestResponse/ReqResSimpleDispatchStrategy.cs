using System;
using System.Collections.Concurrent;
using Acquaintance.Logging;

namespace Acquaintance.RequestResponse
{
    public class ReqResSimpleDispatchStrategy : IReqResChannelDispatchStrategy
    {
        private readonly ConcurrentDictionary<string, IReqResChannel> _reqResChannels;

        public ReqResSimpleDispatchStrategy()
        {
            _reqResChannels = new ConcurrentDictionary<string, IReqResChannel>();
        }

        public IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string topic, ILogger log)
        {
            var key = GetReqResKey(typeof(TRequest), typeof(TResponse), topic);
            if (!_reqResChannels.ContainsKey(key))
            {
                var newChannel = CreateChannel<TRequest, TResponse>(log);
                _reqResChannels.TryAdd(key, newChannel);
            }
            var channel = _reqResChannels[key] as IReqResChannel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Channel is missing or has incorrect type");
            return channel;
        }

        public IReqResChannel<TRequest, TResponse> GetExistingChannel<TRequest, TResponse>(string topic)
        {
            var key = GetReqResKey(typeof(TRequest), typeof(TResponse), topic);
            if (!_reqResChannels.ContainsKey(key))
                return null;
            return _reqResChannels[key] as IReqResChannel<TRequest, TResponse>;
        }

        public void Dispose()
        {
            foreach (var channel in _reqResChannels.Values)
                channel.Dispose();
            _reqResChannels.Clear();
        }

        private IReqResChannel<TRequest, TResponse> CreateChannel<TRequest, TResponse>(ILogger log)
        {
            return new RequestResponseChannel<TRequest, TResponse>(log);
        }

        private static string GetReqResKey(Type requestType, Type responseType, string name)
        {
            return $"Request={requestType.AssemblyQualifiedName}:Response={responseType.AssemblyQualifiedName}:Name={name ?? string.Empty}";
        }
    }
}
