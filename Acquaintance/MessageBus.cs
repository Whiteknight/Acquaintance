using Acquaintance.Logging;
using Acquaintance.PubSub;
using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Acquaintance
{
    public sealed class MessageBus : IMessageBus
    {
        private readonly ILogger _logger;
        private readonly MessagingWorkerThreadPool _threadPool;
        private readonly IPubSubChannelDispatchStrategy _pubSubStrategy;
        private readonly IPubSubChannelDispatchStrategy _eavesdropStrategy;
        private readonly IReqResChannelDispatchStrategy _requestResponseStrategy;
        private readonly IReqResChannelDispatchStrategy _scatterGatherStrategy;
        private readonly ConcurrentDictionary<Guid, IMessageBusModule> _modules;

        public MessageBus(ILogger logger = null)
        {
            _logger = logger ?? CreateDefaultLogger();
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
            _logger.Debug("Starting {0} workers", numThreads);
            _threadPool.StartFreeWorkers(numThreads);
        }

        public void StopWorkers()
        {
            _logger.Debug("Stopping all workers");
            _threadPool.StopFreeWorkers();
            _threadPool.StopAllDedicatedWorkers();
        }

        public int StartDedicatedWorkerThread()
        {
            int id = _threadPool.StartDedicatedWorker();
            _logger.Debug("Starting dedicated worker thread {0}", id);
            return id;
        }

        public void StopDedicatedWorkerThread(int id)
        {
            _logger.Debug("Stopping dedicated worker thread {0}", id);
            _threadPool.StopDedicatedWorker(id);
        }

        public IDisposable AddModule(IMessageBusModule module)
        {
            Guid id = Guid.NewGuid();
            _logger.Debug("Adding module Id={0} Type={1}", id, module.GetType().Name);
            module.Attach(this);
            bool added = _modules.TryAdd(id, module);
            if (!added)
                return null;

            _logger.Debug("Starting module Id={0} Type={1}", id, module.GetType().Name);
            module.Start();
            return new ModuleToken(this, id);
        }

        public void Publish<TPayload>(string name, TPayload payload)
        {
            foreach (var channel in _pubSubStrategy.GetExistingChannels<TPayload>(name))
            {
                _logger.Debug("Publishing message Type={0} ChannelName={1} to channel Id={2}", typeof(TPayload).FullName, name, channel.Id);
                channel.Publish(payload);
            }
        }

        public IDisposable Subscribe<TPayload>(string name, ISubscription<TPayload> subscription)
        {
            var channel = _pubSubStrategy.GetChannelForSubscription<TPayload>(name);
            _logger.Debug("Adding subscription of type Type={0} ChannelName={1} to channel Id={2}", typeof(TPayload).FullName, name, channel.Id);
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
            {
                _logger.Debug("Requesting RequestType={0} ResponseType={1} ChannelName={2} to channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
                waiters.AddRange(channel.Request(request));
            }

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
                _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, with {3} responses", typeof(TRequest).FullName, typeof(TResponse).FullName, name, conversation.Responses.Count);
                foreach (var channel in eavesdropChannels)
                {
                    _logger.Debug("Eavesdropping on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
                    channel.Publish(conversation);
                }
            }

            return new GatheredResponse<TResponse>(responses);
        }

        public IDisposable Listen<TRequest, TResponse>(string name, IListener<TRequest, TResponse> listener)
        {
            var channel = _requestResponseStrategy.GetChannelForSubscription<TRequest, TResponse>(name);
            _logger.Debug("Listening RequestType={0} ResponseType={1} ChannelName={2} to channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
            return channel.Listen(listener);
        }

        public IDisposable Eavesdrop<TRequest, TResponse>(string name, ISubscription<Conversation<TRequest, TResponse>> subscription)
        {
            var channel = _eavesdropStrategy.GetChannelForSubscription<Conversation<TRequest, TResponse>>(name);
            _logger.Debug("Eavesdrop on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
            return channel.Subscribe(subscription);
        }

        public IDisposable Participate<TRequest, TResponse>(string name, IListener<TRequest, TResponse> listener)
        {
            var channel = _scatterGatherStrategy.GetChannelForSubscription<TRequest, TResponse>(name);
            _logger.Debug("Participating on RequestType={0} ResponseType={1} ChannelName={2}, on channel Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, name, channel.Id);
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

            _logger.Debug("Stopping module Id={0} Type={1}", id, module.GetType().Name);
            module.Stop();

            _logger.Debug("Removing module Id={0} Type={1}", id, module.GetType().Name);
            module.Unattach();
        }

        private static ILogger CreateDefaultLogger()
        {
#if DEBUG
            return new DelegateLogger(s => Debug.WriteLine(s));
#else
            return new SilentLogger();
#endif
        }
    }
}
