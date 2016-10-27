﻿using Acquaintance.Dispatching;
using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using System;
using System.Collections.Generic;

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

        public IBrokeredResponse<TResponse> Request<TRequest, TResponse>(string name, TRequest request)
        {
            var responses = new List<TResponse>();
            // TODO: Be able to specify a timeout for this operation to complete.
            // TODO: Keep track of how much time is spent on each channel, and subtract that from the time available to
            // the next channel
            foreach (var channel in _reqResStrategy.GetExistingChannels<TRequest, TResponse>(name))
                responses.AddRange(channel.Request(request));
            return new BrokeredResponse<TResponse>(responses);
        }

        public IDisposable Subscribe<TRequest, TResponse>(string name, Func<TRequest, TResponse> subscriber, Func<TRequest, bool> filter, SubscribeOptions options = null)
        {
            var channel = _reqResStrategy.GetChannelForSubscription<TRequest, TResponse>(name);
            return channel.Subscribe(subscriber, filter, options ?? SubscribeOptions.Default);
        }

        public void RunEventLoop(Func<bool> shouldStop = null, int timeoutMs = 500)
        {
            if (shouldStop == null)
                shouldStop = () => false;
            var threadContext = _threadPool.GetCurrentThread();
            while (!shouldStop() && !threadContext.ShouldStop)
            {
                threadContext.WaitForEvent(timeoutMs);
                var action = threadContext.GetAction();
                action?.Execute(threadContext);
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
