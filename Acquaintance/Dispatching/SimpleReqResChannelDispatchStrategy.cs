﻿using System;
using System.Collections.Generic;
using System.Linq;
using Acquaintance.RequestResponse;
using Acquaintance.Threading;

namespace Acquaintance.Dispatching
{
    public class SimpleReqResChannelDispatchStrategy : IReqResChannelDispatchStrategy
    {
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly Dictionary<string, IReqResChannel> _reqResChannels;

        public SimpleReqResChannelDispatchStrategy(MessagingWorkerThreadPool threadPool)
        {
            _threadPool = threadPool;
            _reqResChannels = new Dictionary<string, IReqResChannel>();
        }

        private string GetReqResKey(Type requestType, Type responseType, string name)
        {
            return string.Format("Request={0}:Response={1}:Name={2}", requestType.Name, responseType.Name, name ?? string.Empty);
        }

        public IReqResChannel<TRequest, TResponse> GetChannelForSubscription<TRequest, TResponse>(string name) where TRequest : IRequest<TResponse>
        {
            string key = GetReqResKey(typeof(TRequest), typeof(TResponse), name);
            if (!_reqResChannels.ContainsKey(key))
                _reqResChannels.Add(key, new ReqResChannel<TRequest, TResponse>(_threadPool));
            var channel = _reqResChannels[key] as IReqResChannel<TRequest, TResponse>;
            if (channel == null)
                throw new Exception("Channel has incorrect type");
            return channel;
        }

        public IEnumerable<IReqResChannel<TRequest, TResponse>> GetExistingChannels<TRequest, TResponse>(string name) where TRequest : IRequest<TResponse>
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
    }
}