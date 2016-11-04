using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Acquaintance
{
    public sealed class MessageBus : IMessageBus
    {
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly IPubSubChannelDispatchStrategy _pubSubStrategy;
        private readonly IPubSubChannelDispatchStrategy _eavesdropStrategy;
        private readonly IReqResChannelDispatchStrategy _requestResponseStrategy;
        private readonly IReqResChannelDispatchStrategy _scatterGatherStrategy;
        private readonly ConcurrentDictionary<Guid, IMessageBusModule> _modules;

        public MessageBus()
        {
            _threadPool = new MessagingWorkerThreadPool();
            _pubSubStrategy = new PubSubChannelDispatchStrategy();
            _eavesdropStrategy = new PubSubChannelDispatchStrategy();
            _requestResponseStrategy = new RequestResponseChannelDispatchStrategy(true);
            _scatterGatherStrategy = new RequestResponseChannelDispatchStrategy(false);
            SubscriptionFactory = new SubscriptionFactory(_threadPool);
            ListenerFactory = new ListenerFactory(_threadPool);
            _modules = new ConcurrentDictionary<Guid, IMessageBusModule>();
        }

        public SubscriptionFactory SubscriptionFactory { get; }
        public ListenerFactory ListenerFactory { get; }

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

        public IDisposable AddModule(IMessageBusModule module)
        {
            Guid id = Guid.NewGuid();
            module.Attach(this);
            bool added = _modules.TryAdd(id, module);
            if (!added)
                return null;

            module.Start();
            return new ModuleToken(this, id);
        }

        public void Publish<TPayload>(string name, TPayload payload)
        {
            foreach (var channel in _pubSubStrategy.GetExistingChannels<TPayload>(name))
                channel.Publish(payload);
        }

        public IDisposable Subscribe<TPayload>(string name, ISubscription<TPayload> subscription)
        {
            var channel = _pubSubStrategy.GetChannelForSubscription<TPayload>(name);
            return channel.Subscribe(subscription);
        }

        public TResponse Request<TRequest, TResponse>(string name, TRequest request)
        {
            return RequestInternal<TRequest, TResponse>(name, request, _requestResponseStrategy).Responses.SingleOrDefault();
        }

        public IGatheredResponse<TResponse> Scatter<TRequest, TResponse>(string name, TRequest request)
        {
            return RequestInternal<TRequest, TResponse>(name, request, _scatterGatherStrategy);
        }

        private IGatheredResponse<TResponse> RequestInternal<TRequest, TResponse>(string name, TRequest request, IReqResChannelDispatchStrategy strategy)
        {
            var waiters = new List<IDispatchableRequest<TResponse>>();
            // TODO: Be able to specify a timeout for this operation to complete.
            // TODO: Keep track of how much time is spent on each channel, and subtract that from the time available to
            // the next channel
            foreach (var channel in strategy.GetExistingChannels<TRequest, TResponse>(name))
                waiters.AddRange(channel.Request(request));

            List<TResponse> responses = new List<TResponse>();
            foreach (var waiter in waiters)
            {
                bool complete = waiter.WaitForResponse();
                responses.Add(complete ? waiter.Response : default(TResponse));
                waiter.Dispose();
            }

            var eavesdropChannels = _eavesdropStrategy.GetExistingChannels<Conversation<TRequest, TResponse>>(name).ToList();
            if (eavesdropChannels.Any())
            {
                var conversation = new Conversation<TRequest, TResponse>(request, responses);
                foreach (var channel in eavesdropChannels)
                    channel.Publish(conversation);
            }

            return new GatheredResponse<TResponse>(responses);
        }

        public IDisposable Listen<TRequest, TResponse>(string name, IListener<TRequest, TResponse> listener)
        {
            var channel = _requestResponseStrategy.GetChannelForSubscription<TRequest, TResponse>(name);
            return channel.Listen(listener);
        }

        public IDisposable Eavesdrop<TRequest, TResponse>(string name, ISubscription<Conversation<TRequest, TResponse>> subscription)
        {
            var channel = _eavesdropStrategy.GetChannelForSubscription<Conversation<TRequest, TResponse>>(name);
            return channel.Subscribe(subscription);
        }

        public IDisposable Participate<TRequest, TResponse>(string name, IListener<TRequest, TResponse> listener)
        {
            var channel = _scatterGatherStrategy.GetChannelForSubscription<TRequest, TResponse>(name);
            return channel.Listen(listener);
        }

        public void RunEventLoop(Func<bool> shouldStop = null, int timeoutMs = 500)
        {
            if (shouldStop == null)
                shouldStop = () => false;
            var threadContext = _threadPool.GetCurrentThreadContext();
            while (!shouldStop() && !threadContext.ShouldStop)
            {
                var action = threadContext.GetAction(timeoutMs);
                action?.Execute(threadContext);
            }
        }

        public void EmptyActionQueue(int max)
        {
            var threadContext = _threadPool.GetCurrentThreadContext();
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
            _requestResponseStrategy.Dispose();
            _eavesdropStrategy.Dispose();
            _scatterGatherStrategy.Dispose();
            _threadPool.Dispose();
            foreach (var module in _modules.Values)
            {
                module.Dispose();
            }
        }

        private class ModuleToken : IDisposable
        {
            private readonly MessageBus _messageBus;
            private readonly Guid _moduleId;

            public ModuleToken(MessageBus messageBus, Guid moduleId)
            {
                _messageBus = messageBus;
                _moduleId = moduleId;
            }

            public void Dispose()
            {
                _messageBus.RemoveModule(_moduleId);
            }
        }

        private void RemoveModule(Guid id)
        {
            IMessageBusModule module;
            _modules.TryRemove(id, out module);
            module.Stop();
            module.Unattach();
        }
    }
}
