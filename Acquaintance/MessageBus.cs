﻿using System;
using System.Collections.Generic;
using Acquaintance.Dispatching;
using Acquaintance.RequestResponse;
using Acquaintance.Threading;

namespace Acquaintance
{
    public sealed class MessageBus : IMessageBus
    {
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly IPubSubChannelDispatchStrategy _pubSubStrategy;
        private readonly IReqResChannelDispatchStrategy _reqResStrategy;

        public MessageBus()
        {
            _threadPool = new MessagingWorkerThreadPool();
            _pubSubStrategy = new SimplePubSubChannelDispatchStrategy(_threadPool);
            _reqResStrategy = new SimpleReqResChannelDispatchStrategy(_threadPool);
        }

        public void StartWorkers(int numThreads = 2)
        {
            _threadPool.StartFreeWorkers(numThreads);
        }

        public void StopWorkers()
        {
            _threadPool.StopFreeWorkers();
            _threadPool.StopAllDedicatedWorkers();
        }

        public int StartDedicatedWorkerThread()
        {
            return _threadPool.StartDedicatedWorker();
        }

        public void StopDedicatedWorkerThread(int id)
        {
            _threadPool.StopDedicatedWorker(id);
        }

        public void Publish<TPayload>(string name, TPayload payload)
        {
            foreach (var channel in _pubSubStrategy.GetExistingChannels(name, payload))
                channel.Publish(payload);
        }

        public IDisposable Subscribe<TPayload>(string name, Action<TPayload> subscriber, Func<TPayload, bool> filter, SubscribeOptions options = null)
        {
            var channel = _pubSubStrategy.GetChannelForSubscription<TPayload>(name);
            return channel.Subscribe(subscriber, filter, options ?? SubscribeOptions.Default);
        }

        public IBrokeredResponse<TResponse> Request<TRequest, TResponse>(string name, TRequest request, int timeoutMs = 0)
            where TRequest : IRequest<TResponse>
        {
            var responses = new List<TResponse>();
            // TODO: Keep track of how much time is spent on each channel, and subtract that from the time available to
            // the next channel
            foreach (var channel in _reqResStrategy.GetExistingChannels<TRequest, TResponse>(name))
                responses.AddRange(channel.Request(request));
            return new BrokeredResponse<TResponse>(responses);
        }

        //public IBrokeredResponse<object> Request(string name, Type requestType, object request)
        //{
        //    // TODO: Cache these lookups. 
        //    var requestInterface = requestType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
        //    if (requestInterface == null)
        //        return new BrokeredResponse<object>(new List<object>());

        //    var responseType = requestInterface.GetGenericArguments().Single();
        //    var method = _reqResStrategy.GetType().GetMethod("GetExistingChannels").MakeGenericMethod(requestType, responseType);

        //    var channels = method.Invoke(_reqResStrategy, new object[] { name });
        //    var channelType = typeof(IReqResChannel<,>).MakeGenericType(requestType, responseType);
        //    var responses = new List<object>();
        //    foreach (var channel in channels)
        //    {
        //        if (channelType.IsInstanceOfType(channel))
        //        {
        //            chan
        //        }
        //    }
        //    if (!)
        //        return new BrokeredResponse<object>(new List<object>());

        //    return channelType.GetMethod("Request").Invoke(channel, new[] { request }) as IBrokeredResponse<object>;
        //}

        public IDisposable Subscribe<TRequest, TResponse>(string name, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter, SubscribeOptions options = null)
            where TRequest : IRequest<TResponse>
        {
            var channel = _reqResStrategy.GetChannelForSubscription<TRequest, TResponse>(name);
            return channel.Subscribe(subscriber, filter, options ?? SubscribeOptions.Default);
        }

        public void RunEventLoop(Func<bool> shouldStop = null, int timeoutMs = 500)
        {
            if (shouldStop == null)
                shouldStop = () => false;
            var threadContext = _threadPool.GetCurrentThread();
            while (!shouldStop())
            {
                threadContext.WaitForEvent(timeoutMs);
                var action = threadContext.GetAction();
                if (action != null)
                    action.Execute(threadContext);
            }
        }

        public void EmptyActionQueue(int max)
        {
            var threadContext = _threadPool.GetCurrentThread();
            for (int i = 0; i < max; i++)
            {
                var action = threadContext.GetAction();
                if (action == null)
                    break;
                action.Execute(threadContext);
            }
        }

        public void Dispose()
        {
            _pubSubStrategy.Dispose();
            _reqResStrategy.Dispose();
            _threadPool.Dispose();
        }
    }
}